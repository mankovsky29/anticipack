using Anticipack.Services.Notifications;
using Foundation;
using UserNotifications;

namespace Anticipack.Platforms.MacCatalyst.Notifications;

public class NotificationManagerService : INotificationManagerService
{
    const string NotificationIdentifier = "packing-reminder";

    public NotificationManagerService()
    {
        UNUserNotificationCenter.Current.Delegate = new NotificationReceiver();
    }

    public async Task<bool> RequestPermissionAsync()
    {
        var (approved, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge);
        return approved;
    }

    public void ScheduleNotification(string title, string message, DateTime notifyTime)
    {
        CancelScheduledNotification();

        var content = new UNMutableNotificationContent
        {
            Title = title,
            Body = message,
            Badge = 1,
            Sound = UNNotificationSound.Default
        };

        var components = new NSDateComponents
        {
            Year = notifyTime.Year,
            Month = notifyTime.Month,
            Day = notifyTime.Day,
            Hour = notifyTime.Hour,
            Minute = notifyTime.Minute,
            Second = notifyTime.Second
        };

        var trigger = UNCalendarNotificationTrigger.CreateTrigger(components, false);
        var request = UNNotificationRequest.FromIdentifier(NotificationIdentifier, content, trigger);

        UNUserNotificationCenter.Current.AddNotificationRequest(request, err =>
        {
            if (err != null)
                System.Diagnostics.Debug.WriteLine($"[PackingReminder] Failed to schedule: {err}");
        });
    }

    public void CancelScheduledNotification()
    {
        UNUserNotificationCenter.Current.RemovePendingNotificationRequests(
            new[] { NotificationIdentifier });
    }
}
