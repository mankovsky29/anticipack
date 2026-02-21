namespace Anticipack.Services.Payment;

/// <summary>
/// Facade interface that orchestrates payment operations across providers (SRP).
/// Components should depend on this interface rather than individual payment providers.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Gets the available payment methods on the current platform.
    /// </summary>
    Task<IReadOnlyList<PaymentMethod>> GetAvailableMethodsAsync();

    /// <summary>
    /// Gets product information with localized prices from the store.
    /// </summary>
    Task<IReadOnlyList<ProductInfo>> GetProductsAsync();

    /// <summary>
    /// Purchases a product using the native store billing.
    /// </summary>
    Task<PurchaseResult> PurchaseProductAsync(string productId, ProductType type);

    /// <summary>
    /// Processes a PayPal payment for a custom amount (donations).
    /// </summary>
    Task<PurchaseResult> ProcessPayPalPaymentAsync(decimal amount, string description = "");

    /// <summary>
    /// Restores previously completed purchases.
    /// </summary>
    Task<IReadOnlyList<PurchaseResult>> RestorePurchasesAsync();

    /// <summary>
    /// Event raised after a successful purchase for premium status refresh.
    /// </summary>
    event EventHandler<PurchaseResult>? PurchaseCompleted;
}
