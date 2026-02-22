namespace Anticipack.API.Models;

public class PackingHistoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ActivityId { get; set; } = string.Empty;
    public DateTime CompletedDate { get; set; }
    public int TotalItems { get; set; }
    public int PackedItems { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // Navigation property
    public PackingActivity? Activity { get; set; }
}
