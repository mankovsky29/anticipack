namespace Anticipack.Services.Payment;

/// <summary>
/// Interface for PayPal payment processing (ISP).
/// Handles web-based PayPal checkout as an alternative payment method.
/// </summary>
public interface IPayPalService
{
    /// <summary>
    /// Gets whether PayPal is available and configured.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Initiates a PayPal payment for the given amount.
    /// Opens a browser-based PayPal checkout flow.
    /// </summary>
    Task<PurchaseResult> ProcessPaymentAsync(decimal amount, string currency = "USD", string description = "");
}
