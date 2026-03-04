using Anticipack.Storage.Repositories;

namespace Anticipack.Services;

/// <summary>
/// Archives activities that haven't been packed within the configured threshold.
/// </summary>
public sealed class AutoArchiveService : IAutoArchiveService
{
    private const string PreferenceKey = "AutoArchiveDays";
    private const int DefaultDays = 7;

    private readonly IPackingActivityRepository _activityRepository;

    public AutoArchiveService(IPackingActivityRepository activityRepository)
    {
        _activityRepository = activityRepository;
    }

    /// <inheritdoc/>
    public int AutoArchiveDays =>
        Preferences.Default.Get(PreferenceKey, DefaultDays);

    /// <inheritdoc/>
    public void SaveAutoArchiveDays(int days)
    {
        Preferences.Default.Set(PreferenceKey, Math.Max(0, days));
    }

    /// <inheritdoc/>
    public async Task RunAutoArchiveAsync()
    {
        var days = AutoArchiveDays;
        if (days <= 0)
            return;

        var activities = await _activityRepository.GetAllAsync();
        var cutoff = DateTime.Now.AddDays(-days);

        foreach (var activity in activities)
        {
            // Only archive activities that have been packed at least once and have gone stale
            if (!activity.IsArchived
                && activity.LastPacked > DateTime.MinValue
                && activity.LastPacked < cutoff)
            {
                activity.IsArchived = true;
                await _activityRepository.AddOrUpdateAsync(activity);
            }
        }
    }
}
