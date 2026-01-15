namespace Anticipack.API.Models;

public class PackingActivity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime LastPacked { get; set; } = DateTime.UtcNow;
    public int RunCount { get; set; }
    public bool IsShared { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User? User { get; set; }
    public List<PackingItem> Items { get; set; } = new();
}

public class PackingItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ActivityId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsPacked { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public PackingActivity? Activity { get; set; }
}
