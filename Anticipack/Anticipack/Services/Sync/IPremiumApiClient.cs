using Anticipack.Services.Sync.Dto;

namespace Anticipack.Services.Sync;

/// <summary>
/// API client interface for premium subscription validation.
/// Separated from sync operations to allow independent premium status checks.
/// </summary>
public interface IPremiumApiClient
{
    /// <summary>
    /// Validates the user's premium subscription status.
    /// </summary>
    Task<PremiumStatusDto> ValidatePremiumStatusAsync(CancellationToken cancellationToken = default);
}
