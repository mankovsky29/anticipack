namespace Anticipack.Services.Sync.Dto;

/// <summary>
/// DTO for sync operation response.
/// </summary>
public class SyncResponseDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime ServerTimestamp { get; set; }
    public int ActivitiesProcessed { get; set; }
    public int ItemsProcessed { get; set; }
    public int HistoryEntriesProcessed { get; set; }
    public List<SyncConflictDto>? Conflicts { get; set; }
}

/// <summary>
/// DTO representing a sync conflict.
/// </summary>
public class SyncConflictDto
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ConflictType { get; set; } = string.Empty;
    public DateTime LocalModifiedAt { get; set; }
    public DateTime ServerModifiedAt { get; set; }
}
