namespace Anticipack.API.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public AuthProvider AuthProvider { get; set; }
    public string? ExternalAuthId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public List<PackingActivity> Activities { get; set; } = new();
    public UserSettings? Settings { get; set; }
}

public enum AuthProvider
{
    Google,
    Apple,
    Email
}
