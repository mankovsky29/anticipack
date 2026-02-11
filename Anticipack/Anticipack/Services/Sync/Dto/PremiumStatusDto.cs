namespace Anticipack.Services.Sync.Dto;

/// <summary>
/// DTO for premium subscription status response.
/// </summary>
public class PremiumStatusDto
{
    public bool IsPremium { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? SubscriptionTier { get; set; }
    public bool IsTrialActive { get; set; }
    public int? DaysRemaining { get; set; }
}
