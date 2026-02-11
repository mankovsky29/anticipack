using Anticipack.Services.Sync.Dto;

namespace Anticipack.Services.Sync;

/// <summary>
/// API client interface for sync operations.
/// Handles HTTP communication with the sync server.
/// </summary>
public interface ISyncApiClient : IPremiumApiClient
{
    /// <summary>
    /// Uploads user data to the server.
    /// </summary>
    Task<SyncResponseDto> UploadDataAsync(SyncDataDto data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads user data from the server.
    /// </summary>
    Task<SyncDataDto?> DownloadDataAsync(DateTime? lastSyncTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the server's last modified timestamp for the user's data.
    /// </summary>
    Task<DateTime?> GetServerLastModifiedAsync(CancellationToken cancellationToken = default);
}
