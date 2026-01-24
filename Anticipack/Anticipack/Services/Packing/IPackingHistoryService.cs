namespace Anticipack.Services.Packing
{
    public interface IPackingHistoryService
    {
        /// <summary>
        /// Records a completed packing session
        /// </summary>
        Task RecordPackingSessionAsync(string activityId, DateTime startTime, DateTime endTime, int totalItems, int packedItems);
        
        /// <summary>
        /// Gets the average packing time for an activity
        /// </summary>
        Task<TimeSpan?> GetAveragePackingTimeAsync(string activityId);
        
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
        /// Compares current packing time with the average
        /// </summary>
        Task<PackingTimeComparison> CompareWithAverageAsync(string activityId, TimeSpan currentDuration);
        
        /// <summary>
        /// Estimates remaining time based on historical data
        /// </summary>
        Task<TimeSpan> EstimateRemainingTimeAsync(string activityId, int itemsRemaining);
    }
    
    public class PackingTimeComparison
    {
        public bool IsFaster { get; set; }
        public TimeSpan Difference { get; set; }
        public string FormattedDifference { get; set; } = string.Empty;
    }
}
