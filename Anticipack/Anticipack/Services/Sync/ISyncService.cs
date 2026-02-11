namespace Anticipack.Services.Sync;

/// <summary>
/// Service interface for synchronizing local data with the server.
/// Only available for premium users.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Gets the current sync status.
    /// </summary>
    SyncStatus Status { get; }

    /// <summary>
    /// Event raised when sync status changes.
    /// </summary>
    event EventHandler<SyncStatus>? SyncStatusChanged;

    /// <summary>
    /// Checks if the current user has premium access for sync features.
    /// </summary>
    Task<bool> IsPremiumUserAsync();

    /// <summary>
    /// Uploads all local data to the server.
    /// Requires premium subscription.
    /// </summary>
    Task<SyncResult> UploadDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads all data from the server and restores locally.
    /// Requires premium subscription.
    /// </summary>
    Task<SyncResult> DownloadDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a bidirectional sync, merging local and server data.
    /// Requires premium subscription.
    /// </summary>
    Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last sync timestamp.
    /// </summary>
    Task<DateTime?> GetLastSyncTimeAsync();
}

/// <summary>
/// Represents the current synchronization status.
/// </summary>
public enum SyncStatus
{
    Idle,
    Syncing,
    Uploading,
    Downloading,
    Error,
    NotPremium
}

/// <summary>
/// Represents the result of a sync operation.
/// </summary>
public record SyncResult(
    bool Success,
    string? ErrorMessage = null,
    int ActivitiesSynced = 0,
    int ItemsSynced = 0,
    int HistoryEntriesSynced = 0,
    DateTime? SyncTime = null);
