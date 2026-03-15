using Android;

namespace Anticipack.Platforms.Android.Notifications;

public class NotificationPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        OperatingSystem.IsAndroidVersionAtLeast(33)
            ? new[] { (Manifest.Permission.PostNotifications, true) }
            : Array.Empty<(string, bool)>();
}
