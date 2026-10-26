using SQLite;

namespace Anticipack.Storage
{
    public class PackingHistoryEntry
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string ActivityId { get; set; } = string.Empty; // Foreign key to PackingActivity
        
        public DateTime CompletedDate { get; set; }
        
        public int TotalItems { get; set; }
        
        public int PackedItems { get; set; }
        
        /// <summary>
        /// Duration in seconds
        /// </summary>
        public int DurationSeconds { get; set; }
        
        /// <summary>
        /// Start time of the packing session
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// End time of the packing session
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Time actually spent packing (idle gaps between checks are capped).
        /// Null for sessions recorded before active time was measured.
        /// </summary>
        public int? ActiveDurationSeconds { get; set; }

        /// <summary>
        /// Comma-separated ids of items left unpacked when the session was finished.
        /// Null for sessions recorded before skipped items were tracked.
        /// </summary>
        public string? SkippedItemIds { get; set; }
    }
}
