namespace Anticipack.Services.Notifications;

/// <summary>
/// Schedules a single local notification when a packing activity has been inactive for 24 hours.
/// </summary>
public interface IPackingReminderService
{
    /// <summary>
    /// Requests notification permission, cancels any pending reminder,
    /// then schedules a new one based on current activity state.
    /// </summary>
    Task ScheduleReminderIfNeededAsync();

    /// <summary>
    /// Cancels any pending packing reminder notification.
    /// </summary>
    void CancelReminder();
}
