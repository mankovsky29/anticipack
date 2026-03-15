using UserNotifications;

namespace Anticipack.Platforms.iOS.Notifications;

public class NotificationReceiver : UNUserNotificationCenterDelegate
{
    // Present the notification as a banner when the app is in the foreground.
    public override void WillPresentNotification(UNUserNotificationCenter center,
        UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        var options = OperatingSystem.IsIOSVersionAtLeast(14)
            ? UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.Sound
            : UNNotificationPresentationOptions.Alert | UNNotificationPresentationOptions.Sound;

        completionHandler(options);
    }

    public override void DidReceiveNotificationResponse(UNUserNotificationCenter center,
        UNNotificationResponse response, Action completionHandler)
    {
        completionHandler();
    }
}
