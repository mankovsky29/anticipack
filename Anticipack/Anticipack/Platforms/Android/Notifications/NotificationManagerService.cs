using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Anticipack.Services.Notifications;

namespace Anticipack.Platforms.Android.Notifications;

public class NotificationManagerService : INotificationManagerService
{
    const string ChannelId = "packing_reminders";
    const string ChannelName = "Packing Reminders";
    const string ChannelDescription = "Reminders to finish packing activities.";
    const int ReminderId = 2001;

    NotificationManagerCompat _compatManager;
    bool _channelInitialized;

    public NotificationManagerService()
    {
        CreateNotificationChannel();
        _compatManager = NotificationManagerCompat.From(Platform.AppContext);
    }

    public async Task<bool> RequestPermissionAsync()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = await Permissions.RequestAsync<NotificationPermission>();
            return status == PermissionStatus.Granted;
        }
        return true;
    }

    public void ScheduleNotification(string title, string message, DateTime notifyTime)
    {
        if (!_channelInitialized)
            CreateNotificationChannel();

        var intent = new Intent(Platform.AppContext, typeof(PackingAlarmHandler));
        intent.PutExtra(PackingAlarmHandler.TitleKey, title);
        intent.PutExtra(PackingAlarmHandler.MessageKey, message);
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

        var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
            ? PendingIntentFlags.CancelCurrent | PendingIntentFlags.Immutable
            : PendingIntentFlags.CancelCurrent;

        var pendingIntent = PendingIntent.GetBroadcast(Platform.AppContext, ReminderId, intent, pendingIntentFlags);
        long triggerTime = ToEpochMilliseconds(notifyTime);

        var alarmManager = (AlarmManager)Platform.AppContext.GetSystemService(Context.AlarmService)!;
        alarmManager.Set(AlarmType.RtcWakeup, triggerTime, pendingIntent);
    }

    public void CancelScheduledNotification()
    {
        var intent = new Intent(Platform.AppContext, typeof(PackingAlarmHandler));
        var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
            ? PendingIntentFlags.CancelCurrent | PendingIntentFlags.Immutable
            : PendingIntentFlags.CancelCurrent;

        var pendingIntent = PendingIntent.GetBroadcast(Platform.AppContext, ReminderId, intent, pendingIntentFlags);
        var alarmManager = (AlarmManager)Platform.AppContext.GetSystemService(Context.AlarmService)!;
        alarmManager.Cancel(pendingIntent);
    }

    public void ShowNotification(string title, string message)
    {
        var intent = new Intent(Platform.AppContext, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

        var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
            ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            : PendingIntentFlags.UpdateCurrent;

        var pendingIntent = PendingIntent.GetActivity(Platform.AppContext, ReminderId, intent, pendingIntentFlags);

        var builder = new NotificationCompat.Builder(Platform.AppContext, ChannelId)
            .SetContentIntent(pendingIntent)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetAutoCancel(true)
            .SetPriority(NotificationCompat.PriorityDefault);

        _compatManager.Notify(ReminderId, builder.Build());
    }

    void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Default)
            {
                Description = ChannelDescription
            };
            var manager = (NotificationManager)Platform.AppContext.GetSystemService(Context.NotificationService)!;
            manager.CreateNotificationChannel(channel);
            _channelInitialized = true;
        }
    }

    static long ToEpochMilliseconds(DateTime dateTime)
    {
        var utcTime = TimeZoneInfo.ConvertTimeToUtc(dateTime);
        var epochDiff = (new DateTime(1970, 1, 1) - DateTime.MinValue).TotalSeconds;
        return utcTime.AddSeconds(-epochDiff).Ticks / 10000;
    }
}
