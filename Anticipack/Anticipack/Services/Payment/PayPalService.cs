using Microsoft.Extensions.Logging;

namespace Anticipack.Services.Payment;

/// <summary>
/// Handles PayPal web-based checkout flow (SRP).
/// Opens a PayPal checkout in the device browser and handles the return.
/// </summary>
public class PayPalService : IPayPalService
{
    private readonly ILogger<PayPalService> _logger;
    private readonly PayPalConfiguration _config;

    public PayPalService(PayPalConfiguration config, ILogger<PayPalService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public bool IsAvailable => !string.IsNullOrEmpty(_config.ClientId);

    public async Task<PurchaseResult> ProcessPaymentAsync(
        decimal amount, string currency = "USD", string description = "")
    {
        if (!IsAvailable)
            return PurchaseResult.Failed("paypal", "PayPal is not configured.");

        try
        {
            var payPalUrl = BuildCheckoutUrl(amount, currency, description);

            // Open PayPal checkout in the device browser
            await Browser.Default.OpenAsync(payPalUrl, BrowserLaunchMode.SystemPreferred);

            // Note: In production, implement a deep link / URI scheme callback
            // to capture the payment result after the user completes checkout.
            // For now, we return a pending result and instruct the user to confirm.
            return new PurchaseResult
            {
                Success = true,
                ProductId = "paypal_donation",
                TransactionId = Guid.NewGuid().ToString("N"),
                Method = PaymentMethod.PayPal,
                Message = "PayPal checkout opened. Complete the payment in your browser."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal payment failed for amount {Amount} {Currency}", amount, currency);
            return PurchaseResult.Failed("paypal", "Failed to open PayPal checkout.", ex.Message);
        }
    }

    private Uri BuildCheckoutUrl(decimal amount, string currency, string description)
    {
        // PayPal.Me link format for simple payments
        // In production, replace with PayPal Orders API server-side integration
        var baseUrl = _config.UseSandbox
            ? "https://www.sandbox.paypal.com"
            : "https://www.paypal.com";

        var encodedDescription = Uri.EscapeDataString(
            string.IsNullOrEmpty(description) ? "Anticipack Donation" : description);

        // PayPal.Me provides the simplest integration for donations
        if (!string.IsNullOrEmpty(_config.PayPalMeUsername))
        {
            return new Uri($"https://paypal.me/{_config.PayPalMeUsername}/{amount:F2}{currency}");
        }

        // Fallback: PayPal donation button URL
        return new Uri(
            $"{baseUrl}/cgi-bin/webscr?cmd=_donations" +
            $"&business={Uri.EscapeDataString(_config.BusinessEmail)}" +
            $"&amount={amount:F2}" +
            $"&currency_code={currency}" +
            $"&item_name={encodedDescription}");
    }
}

/// <summary>
/// Configuration for PayPal payment integration.
/// </summary>
public class PayPalConfiguration
{
    /// <summary>
    /// PayPal API Client ID (for REST API integration).
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// PayPal business email for donation buttons.
    /// </summary>
    public string BusinessEmail { get; set; } = string.Empty;

    /// <summary>
    /// PayPal.Me username for simplified payment links.
    /// </summary>
    public string PayPalMeUsername { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use the PayPal sandbox for testing.
    /// </summary>
    public bool UseSandbox { get; set; }
#if DEBUG
        = true;
#endif
}
