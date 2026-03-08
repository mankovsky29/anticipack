namespace Anticipack.Services.Notifications;

/// <summary>
/// Cross-platform abstraction for scheduling local notifications.
/// </summary>
public interface INotificationManagerService
{
    /// <summary>Requests the OS notification permission.</summary>
    Task<bool> RequestPermissionAsync();

    /// <summary>Schedules a local notification to fire at <paramref name="notifyTime"/>.</summary>
    void ScheduleNotification(string title, string message, DateTime notifyTime);

    /// <summary>Cancels the pending packing reminder notification.</summary>
    void CancelScheduledNotification();
}
