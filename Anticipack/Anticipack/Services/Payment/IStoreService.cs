namespace Anticipack.Services.Payment;

/// <summary>
/// Interface for native in-app billing via Google Play / Apple App Store (ISP).
/// Handles purchases where Apple Pay and Google Pay are available as payment methods
/// configured by the user on their device.
/// </summary>
public interface IStoreService
{
    /// <summary>
    /// Gets whether the store billing service is available on this device.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Retrieves product information (including localized prices) from the store.
    /// </summary>
    Task<IReadOnlyList<ProductInfo>> GetProductsAsync(IEnumerable<string> productIds);

    /// <summary>
    /// Initiates a purchase through the native store.
    /// </summary>
    Task<PurchaseResult> PurchaseAsync(string productId, ProductType type);

    /// <summary>
    /// Restores previously completed purchases (non-consumables and subscriptions).
    /// </summary>
    Task<IReadOnlyList<PurchaseResult>> RestorePurchasesAsync();
}
