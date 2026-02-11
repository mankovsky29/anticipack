using Anticipack.Services.Sync.Dto;
using Anticipack.Storage;
using Anticipack.Storage.Repositories;
using Microsoft.Extensions.Logging;

namespace Anticipack.Services.Sync;

/// <summary>
/// Main synchronization service that coordinates data sync between local storage and server.
/// Enforces premium subscription requirement to avoid unnecessary API load.
/// </summary>
public class SyncService : ISyncService
{
    private readonly IPremiumService _premiumService;
    private readonly ISyncApiClient _apiClient;
    private readonly IPackingActivityRepository _activityRepository;
    private readonly IPackingItemRepository _itemRepository;
    private readonly IPackingHistoryRepository _historyRepository;
    private readonly ILogger<SyncService> _logger;

    private const string LastSyncTimeKey = "last_sync_time";
    private const string DeviceIdKey = "device_id";

    private SyncStatus _status = SyncStatus.Idle;
    public SyncStatus Status
    {
        get => _status;
        private set
        {
            if (_status != value)
            {
                _status = value;
                SyncStatusChanged?.Invoke(this, _status);
            }
        }
    }

    public event EventHandler<SyncStatus>? SyncStatusChanged;

    public SyncService(
        IPremiumService premiumService,
        ISyncApiClient apiClient,
        IPackingActivityRepository activityRepository,
        IPackingItemRepository itemRepository,
        IPackingHistoryRepository historyRepository,
        ILogger<SyncService> logger)
    {
        _premiumService = premiumService;
        _apiClient = apiClient;
        _activityRepository = activityRepository;
        _itemRepository = itemRepository;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    public async Task<bool> IsPremiumUserAsync()
    {
        return await _premiumService.IsPremiumAsync();
    }

    public async Task<SyncResult> UploadDataAsync(CancellationToken cancellationToken = default)
    {
        if (!await _premiumService.IsPremiumAsync())
        {
            Status = SyncStatus.NotPremium;
            return new SyncResult(false, "Premium subscription required for sync features");
        }

        return await UploadDataInternalAsync(cancellationToken);
    }

    public async Task<SyncResult> DownloadDataAsync(CancellationToken cancellationToken = default)
    {
        if (!await _premiumService.IsPremiumAsync())
        {
            Status = SyncStatus.NotPremium;
            return new SyncResult(false, "Premium subscription required for sync features");
        }

        return await DownloadDataInternalAsync(cancellationToken);
    }

    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await _premiumService.IsPremiumAsync())
        {
            Status = SyncStatus.NotPremium;
            return new SyncResult(false, "Premium subscription required for sync features");
        }

        try
        {
            Status = SyncStatus.Syncing;

            // Upload local changes (skip premium check - already verified)
            var uploadResult = await UploadDataInternalAsync(cancellationToken);
            if (!uploadResult.Success)
            {
                return uploadResult;
            }

            // Download server changes (skip premium check - already verified)
            var downloadResult = await DownloadDataInternalAsync(cancellationToken);

            Status = SyncStatus.Idle;
            return new SyncResult(
                downloadResult.Success,
                downloadResult.ErrorMessage,
                uploadResult.ActivitiesSynced + downloadResult.ActivitiesSynced,
                uploadResult.ItemsSynced + downloadResult.ItemsSynced,
                uploadResult.HistoryEntriesSynced + downloadResult.HistoryEntriesSynced,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
            Status = SyncStatus.Error;
            return new SyncResult(false, ex.Message);
        }
    }

    public Task<DateTime?> GetLastSyncTimeAsync()
    {
        var ticks = Preferences.Get(LastSyncTimeKey, 0L);
        if (ticks > 0)
        {
            return Task.FromResult<DateTime?>(new DateTime(ticks, DateTimeKind.Utc));
        }
        return Task.FromResult<DateTime?>(null);
    }

    /// <summary>
    /// Internal upload method without premium check (for use after premium is already verified).
    /// </summary>
    private async Task<SyncResult> UploadDataInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            Status = SyncStatus.Uploading;

            var syncData = await BuildSyncDataAsync();
            var response = await _apiClient.UploadDataAsync(syncData, cancellationToken);

            if (response.Success)
            {
                await SaveLastSyncTimeAsync(response.ServerTimestamp);
                Status = SyncStatus.Idle;

                return new SyncResult(
                    true,
                    ActivitiesSynced: response.ActivitiesProcessed,
                    ItemsSynced: response.ItemsProcessed,
                    HistoryEntriesSynced: response.HistoryEntriesProcessed,
                    SyncTime: response.ServerTimestamp);
            }

            Status = SyncStatus.Error;
            return new SyncResult(false, response.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed");
            Status = SyncStatus.Error;
            return new SyncResult(false, ex.Message);
        }
    }

    /// <summary>
    /// Internal download method without premium check (for use after premium is already verified).
    /// </summary>
    private async Task<SyncResult> DownloadDataInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            Status = SyncStatus.Downloading;

            var lastSync = await GetLastSyncTimeAsync();
            var serverData = await _apiClient.DownloadDataAsync(lastSync, cancellationToken);

            if (serverData == null)
            {
                Status = SyncStatus.Idle;
                return new SyncResult(true, "No data to download");
            }

            var result = await ApplyServerDataAsync(serverData);
            await SaveLastSyncTimeAsync(serverData.SyncTimestamp);

            Status = SyncStatus.Idle;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed");
            Status = SyncStatus.Error;
            return new SyncResult(false, ex.Message);
        }
    }

    private async Task<SyncDataDto> BuildSyncDataAsync()
    {
        var activities = await _activityRepository.GetAllAsync();
        var syncData = new SyncDataDto
        {
            UserId = await GetOrCreateUserIdAsync(),
            DeviceId = GetOrCreateDeviceId(),
            SyncTimestamp = DateTime.UtcNow,
            Activities = [],
            Items = [],
            HistoryEntries = []
        };

        foreach (var activity in activities)
        {
            syncData.Activities.Add(MapToDto(activity));

            var items = await _itemRepository.GetItemsForActivityAsync(activity.Id);
            syncData.Items.AddRange(items.Select(MapToDto));

            var history = await _historyRepository.GetHistoryForActivityAsync(activity.Id);
            syncData.HistoryEntries.AddRange(history.Select(MapToDto));
        }

        return syncData;
    }

    private async Task<SyncResult> ApplyServerDataAsync(SyncDataDto serverData)
    {
        int activitiesApplied = 0;
        int itemsApplied = 0;
        int historyApplied = 0;

        foreach (var activityDto in serverData.Activities)
        {
            if (activityDto.DeletedAt.HasValue)
            {
                await _activityRepository.DeleteAsync(activityDto.Id);
            }
            else
            {
                var activity = MapFromDto(activityDto);
                await _activityRepository.AddOrUpdateAsync(activity);
            }
            activitiesApplied++;
        }

        foreach (var itemDto in serverData.Items)
        {
            if (itemDto.DeletedAt.HasValue)
            {
                await _itemRepository.DeleteItemAsync(itemDto.Id);
            }
            else
            {
                var item = MapFromDto(itemDto);
                await _itemRepository.AddOrUpdateItemAsync(item);
            }
            itemsApplied++;
        }

        foreach (var historyDto in serverData.HistoryEntries)
        {
            var entry = MapFromDto(historyDto);
            await _historyRepository.AddHistoryEntryAsync(entry);
            historyApplied++;
        }

        return new SyncResult(
            true,
            ActivitiesSynced: activitiesApplied,
            ItemsSynced: itemsApplied,
            HistoryEntriesSynced: historyApplied,
            SyncTime: serverData.SyncTimestamp);
    }

    private Task SaveLastSyncTimeAsync(DateTime syncTime)
    {
        Preferences.Set(LastSyncTimeKey, syncTime.Ticks);
        return Task.CompletedTask;
    }

    private string GetOrCreateDeviceId()
    {
        var deviceId = Preferences.Get(DeviceIdKey, string.Empty);
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            Preferences.Set(DeviceIdKey, deviceId);
        }
        return deviceId;
    }

    private Task<string> GetOrCreateUserIdAsync()
    {
        // TODO: Replace with actual authentication service
        return Task.FromResult(GetOrCreateDeviceId());
    }

    #region Mapping Methods

    private static PackingActivityDto MapToDto(PackingActivity activity) => new()
    {
        Id = activity.Id,
        Name = activity.Name,
        LastPacked = activity.LastPacked,
        RunCount = activity.RunCount,
        IsShared = activity.IsShared,
        IsArchived = activity.IsArchived,
        IsFinished = activity.IsFinished,
        IsRecurring = activity.IsRecurring,
        ModifiedAt = DateTime.UtcNow
    };

    private static PackingItemDto MapToDto(PackingItem item) => new()
    {
        Id = item.Id,
        ActivityId = item.ActivityId,
        Name = item.Name,
        IsPacked = item.IsPacked,
        Category = item.Category,
        Notes = item.Notes,
        SortOrder = item.SortOrder,
        ModifiedAt = DateTime.UtcNow
    };

    private static PackingHistoryEntryDto MapToDto(PackingHistoryEntry entry) => new()
    {
        Id = entry.Id,
        ActivityId = entry.ActivityId,
        CompletedDate = entry.CompletedDate,
        TotalItems = entry.TotalItems,
        PackedItems = entry.PackedItems,
        DurationSeconds = entry.DurationSeconds,
        StartTime = entry.StartTime,
        EndTime = entry.EndTime
    };

    private static PackingActivity MapFromDto(PackingActivityDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        LastPacked = dto.LastPacked,
        RunCount = dto.RunCount,
        IsShared = dto.IsShared,
        IsArchived = dto.IsArchived,
        IsFinished = dto.IsFinished,
        IsRecurring = dto.IsRecurring
    };

    private static PackingItem MapFromDto(PackingItemDto dto) => new()
    {
        Id = dto.Id,
        ActivityId = dto.ActivityId,
        Name = dto.Name,
        IsPacked = dto.IsPacked,
        Category = dto.Category ?? string.Empty,
        Notes = dto.Notes ?? string.Empty,
        SortOrder = dto.SortOrder
    };

    private static PackingHistoryEntry MapFromDto(PackingHistoryEntryDto dto) => new()
    {
        Id = dto.Id,
        ActivityId = dto.ActivityId,
        CompletedDate = dto.CompletedDate,
        TotalItems = dto.TotalItems,
        PackedItems = dto.PackedItems,
        DurationSeconds = dto.DurationSeconds,
        StartTime = dto.StartTime,
        EndTime = dto.EndTime
    };

    #endregion
}
