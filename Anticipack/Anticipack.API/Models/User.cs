using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Anticipack.API.Models;

public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
    public string? ProfilePictureUrl { get; set; }

    public AuthProvider AuthProvider { get; set; }
    public string? ExternalAuthId { get; set; }

    public string? GoogleId { get; set; }
    public string? FacebookId { get; set; }
    public string? AppleId { get; set; }
    public string? TelegramId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public List<RefreshToken> RefreshTokens { get; set; } = new();

    // In-memory app domain navigations (not stored in identity database)
    [NotMapped]
    public List<PackingActivity> Activities { get; set; } = new();

    [NotMapped]
    public UserSettings? Settings { get; set; }
}

public enum AuthProvider
{
    Google,
    Facebook,
    Apple,
    Email,
    Telegram
}
