using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Anticipack.API.Models;

public class RefreshToken
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(256)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; }

    [MaxLength(64)]
    public string? CreatedByIp { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string? RevokedByIp { get; set; }

    public DateTime? RevokedAt { get; set; }

    [MaxLength(256)]
    public string? ReplacedByTokenHash { get; set; }

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ExpiryDate;

    [NotMapped]
    public bool IsActive => !IsRevoked && !IsExpired;

    public User User { get; set; } = default!;
}
