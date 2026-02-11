namespace Anticipack.Services.Sync;

/// <summary>
/// Configuration options for the sync service.
/// </summary>
public class SyncConfiguration
{
    /// <summary>
    /// Base URL for the sync API.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.anticipack.com";

    /// <summary>
    /// Timeout for API requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable automatic sync on app start (premium users only).
    /// </summary>
    public bool AutoSyncOnStart { get; set; } = false;

    /// <summary>
    /// Interval in minutes for automatic background sync (premium users only).
    /// </summary>
    public int AutoSyncIntervalMinutes { get; set; } = 60;
}
