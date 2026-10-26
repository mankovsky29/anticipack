namespace Anticipack.Services.Packing
{
    public interface IPackingHistoryService
    {
        /// <summary>
        /// Records a completed packing session
        /// </summary>
        Task RecordPackingSessionAsync(string activityId, DateTime startTime, DateTime endTime, TimeSpan activeDuration, int totalItems, int packedItems, IReadOnlyCollection<string>? skippedItemIds = null);

        /// <summary>
        /// Finds items of an activity that were repeatedly left unpacked in recent sessions
        /// </summary>
        Task<List<SkippedItemInsight>> GetOftenSkippedItemsAsync(string activityId, IEnumerable<Storage.PackingItem> items);
        
        /// <summary>
        /// Gets how long packing this activity usually takes, or null until a session has been measured
        /// </summary>
        Task<TimeSpan?> GetTypicalPackingTimeAsync(string activityId);

        /// <summary>
        /// Gets the typical packing time of every activity that has measured sessions, keyed by activity id
        /// </summary>
        Task<Dictionary<string, TimeSpan>> GetTypicalPackingTimesAsync();
        
        /// <summary>
        /// Gets recent packing history entries for an activity
        /// </summary>
        Task<List<Storage.PackingHistoryEntry>> GetRecentHistoryAsync(string activityId, int count = 10);
        
        /// <summary>
        /// Gets the last packing session for an activity
        /// </summary>
        Task<Storage.PackingHistoryEntry?> GetLastPackingSessionAsync(string activityId);
        
        /// <summary>
        /// Calculates packing efficiency (percentage of items packed)
        /// </summary>
        double GetPackingEfficiency(int packedItems, int totalItems);
        
        /// <summary>
        /// Compares a session's active time with the typical time, or null when there is nothing to compare with
        /// </summary>
        Task<PackingTimeComparison?> CompareWithTypicalAsync(string activityId, TimeSpan currentDuration);
    }
    
    public record SkippedItemInsight(Storage.PackingItem Item, int SkippedCount, int SessionCount);

    public class PackingTimeComparison
    {
        public TimeSpan Typical { get; set; }

        /// <summary>
        /// Typical minus current duration: positive means the session was faster than usual
        /// </summary>
        public TimeSpan Difference { get; set; }

        public bool IsFaster => Difference > TimeSpan.Zero;
    }
}
