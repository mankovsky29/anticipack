using UserNotifications;

namespace Anticipack.Platforms.MacCatalyst.Notifications;

public class NotificationReceiver : UNUserNotificationCenterDelegate
{
    public override void WillPresentNotification(UNUserNotificationCenter center,
        UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.Sound);
    }

    public override void DidReceiveNotificationResponse(UNUserNotificationCenter center,
        UNNotificationResponse response, Action completionHandler)
    {
        completionHandler();
    }
}
