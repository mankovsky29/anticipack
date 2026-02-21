namespace Anticipack.Services.Payment;

/// <summary>
/// Represents a purchasable product available in the app.
/// </summary>
public class ProductInfo
{
    /// <summary>
    /// Store product identifier (must match Play Console / App Store Connect configuration).
    /// </summary>
    public string ProductId { get; init; } = string.Empty;

    /// <summary>
    /// Display name of the product.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Localized price string from the store (e.g., "$1.99").
    /// </summary>
    public string LocalizedPrice { get; set; } = string.Empty;

    /// <summary>
    /// Price in micro-units for comparison (e.g., 1990000 for $1.99).
    /// </summary>
    public long PriceMicros { get; set; }

    /// <summary>
    /// Currency code (e.g., "USD").
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Description of the product.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Type of product.
    /// </summary>
    public ProductType Type { get; init; }

    /// <summary>
    /// Icon class for UI display (e.g., Font Awesome class).
    /// </summary>
    public string IconClass { get; init; } = string.Empty;
}

/// <summary>
/// Type of purchasable product.
/// </summary>
public enum ProductType
{
    /// <summary>
    /// One-time purchase that is consumed (e.g., tips/donations).
    /// </summary>
    Consumable,

    /// <summary>
    /// One-time purchase that persists (e.g., unlock feature).
    /// </summary>
    NonConsumable,

    /// <summary>
    /// Recurring subscription (e.g., premium monthly).
    /// </summary>
    Subscription
}
