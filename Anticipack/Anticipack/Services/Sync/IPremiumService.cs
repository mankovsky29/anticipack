namespace Anticipack.Services.Sync;

/// <summary>
/// Service interface for managing premium subscription status.
/// Abstracts the subscription validation logic to avoid unnecessary API calls.
/// </summary>
public interface IPremiumService
{
    /// <summary>
    /// Gets whether the current user has an active premium subscription.
    /// Uses cached value when available to minimize API calls.
    /// </summary>
    Task<bool> IsPremiumAsync();

    /// <summary>
    /// Forces a refresh of the premium status from the server.
    /// </summary>
    Task<bool> RefreshPremiumStatusAsync();

    /// <summary>
    /// Gets the subscription expiration date, if available.
    /// </summary>
    Task<DateTime?> GetSubscriptionExpirationAsync();

    /// <summary>
    /// Event raised when premium status changes.
    /// </summary>
    event EventHandler<PremiumStatusChangedEventArgs>? PremiumStatusChanged;
}

/// <summary>
/// Event arguments for premium status changes.
/// </summary>
public class PremiumStatusChangedEventArgs : EventArgs
{
    public bool IsPremium { get; }
    public DateTime? ExpirationDate { get; }

    public PremiumStatusChangedEventArgs(bool isPremium, DateTime? expirationDate = null)
    {
        IsPremium = isPremium;
        ExpirationDate = expirationDate;
    }
}
