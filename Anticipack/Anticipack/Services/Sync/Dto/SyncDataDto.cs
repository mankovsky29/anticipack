namespace Anticipack.Services.Sync.Dto;

/// <summary>
/// Data transfer object containing all user data for synchronization.
/// </summary>
public class SyncDataDto
{
    public string UserId { get; set; } = string.Empty;
    public DateTime SyncTimestamp { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public List<PackingActivityDto> Activities { get; set; } = [];
    public List<PackingItemDto> Items { get; set; } = [];
    public List<PackingHistoryEntryDto> HistoryEntries { get; set; } = [];
}

/// <summary>
/// DTO for PackingActivity.
/// </summary>
public class PackingActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime LastPacked { get; set; }
    public int RunCount { get; set; }
    public bool IsShared { get; set; }
    public bool IsArchived { get; set; }
    public bool IsFinished { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime? DeletedAt { get; set; } // For soft delete tracking
    public DateTime ModifiedAt { get; set; } // For conflict resolution
}

/// <summary>
/// DTO for PackingItem.
/// </summary>
public class PackingItemDto
{
    public string Id { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsPacked { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}

/// <summary>
/// DTO for PackingHistoryEntry.
/// </summary>
public class PackingHistoryEntryDto
{
    public string Id { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public DateTime CompletedDate { get; set; }
    public int TotalItems { get; set; }
    public int PackedItems { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
