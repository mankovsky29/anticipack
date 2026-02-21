namespace Anticipack.Services.Payment;

/// <summary>
/// Supported payment methods.
/// Apple Pay and Google Pay are handled automatically by the native store
/// billing when users have them configured on their devices.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Native in-app purchase via Google Play or Apple App Store.
    /// Supports Google Pay, Apple Pay, credit cards, carrier billing, gift cards, etc.
    /// </summary>
    StoreBilling,

    /// <summary>
    /// PayPal web-based checkout flow.
    /// </summary>
    PayPal
}
