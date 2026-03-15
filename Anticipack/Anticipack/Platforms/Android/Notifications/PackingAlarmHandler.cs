using Android.App;
using Android.Content;

namespace Anticipack.Platforms.Android.Notifications;

[BroadcastReceiver(Enabled = true, Label = "Packing Reminder Alarm Receiver")]
public class PackingAlarmHandler : BroadcastReceiver
{
    public const string TitleKey = "packing_title";
    public const string MessageKey = "packing_message";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Extras == null)
            return;

        string title = intent.GetStringExtra(TitleKey) ?? string.Empty;
        string message = intent.GetStringExtra(MessageKey) ?? string.Empty;

        var service = new NotificationManagerService();
        service.ShowNotification(title, message);
    }
}
