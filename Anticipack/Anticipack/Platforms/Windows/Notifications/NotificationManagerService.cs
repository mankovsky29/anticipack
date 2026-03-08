using Anticipack.Services.Notifications;

namespace Anticipack.Platforms.Windows.Notifications;

/// <summary>
/// Windows no-op — scheduled local notifications are not supported
/// in the unpackaged Windows App SDK.
/// </summary>
public class NotificationManagerService : INotificationManagerService
{
    public Task<bool> RequestPermissionAsync() => Task.FromResult(false);

    public void ScheduleNotification(string title, string message, DateTime notifyTime) { }

    public void CancelScheduledNotification() { }
}
