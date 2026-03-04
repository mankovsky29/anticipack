namespace Anticipack.Services;

/// <summary>
/// Manages the auto-archive preference and applies the rule to stale activities.
/// </summary>
public interface IAutoArchiveService
{
    /// <summary>Days of inactivity before an activity is auto-archived (0 = disabled).</summary>
    int AutoArchiveDays { get; }

    /// <summary>Persists a new threshold value.</summary>
    void SaveAutoArchiveDays(int days);

    /// <summary>Archives all eligible activities based on the current threshold.</summary>
    Task RunAutoArchiveAsync();
}
