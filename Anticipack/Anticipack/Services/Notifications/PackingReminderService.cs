using Anticipack.Resources.Localization;
using Anticipack.Storage.Repositories;

namespace Anticipack.Services.Notifications;

/// <summary>
/// Schedules a single local notification when one or more packing activities
/// have not been updated for 12 hours and are still unfinished.
/// </summary>
public sealed class PackingReminderService : IPackingReminderService
{
    private static readonly TimeSpan ReminderThreshold = TimeSpan.FromHours(12);

    private readonly IPackingActivityRepository _activityRepository;
    private readonly INotificationManagerService _notificationManager;

    public PackingReminderService(
        IPackingActivityRepository activityRepository,
        INotificationManagerService notificationManager)
    {
        _activityRepository = activityRepository;
        _notificationManager = notificationManager;
    }

    /// <inheritdoc/>
    public async Task ScheduleReminderIfNeededAsync()
    {
        var granted = await _notificationManager.RequestPermissionAsync();
        if (!granted)
            return;

        _notificationManager.CancelScheduledNotification();

        var activities = await _activityRepository.GetAllAsync();

        var candidates = activities
            .Where(a => !a.IsFinished && !a.IsArchived && a.LastPacked > DateTime.MinValue)
            .OrderBy(a => a.LastPacked)
            .ToList();

        if (candidates.Count == 0)
            return;

        var now = DateTime.Now;
        var stale = candidates.Where(a => now - a.LastPacked >= ReminderThreshold).ToList();

        DateTime notifyAt;
        string description;

        if (stale.Count > 0)
        {
            notifyAt = now.AddSeconds(5);
            description = stale.Count == 1
                ? string.Format(AppResources.ReminderNotificationBodySingle, stale[0].Name)
                : string.Format(AppResources.ReminderNotificationBodyMultiple, stale.Count);
        }
        else
        {
            var soonest = candidates[0];
            notifyAt = soonest.LastPacked + ReminderThreshold;
            if (notifyAt <= now)
                notifyAt = now.AddSeconds(5);

            description = string.Format(AppResources.ReminderNotificationBodySingle, soonest.Name);
        }

        _notificationManager.ScheduleNotification(
            AppResources.ReminderNotificationTitle,
            description,
            notifyAt);
    }

    /// <inheritdoc/>
    public void CancelReminder()
    {
        _notificationManager.CancelScheduledNotification();
    }
}
