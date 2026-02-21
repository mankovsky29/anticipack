using Microsoft.Extensions.Logging;

namespace Anticipack.Services.Payment;

/// <summary>
/// Orchestrates payment operations across different providers (SRP).
/// Acts as a facade so UI components only depend on a single interface (DIP).
/// New payment methods can be added without modifying this class (OCP)
/// by registering additional providers.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IStoreService _storeService;
    private readonly IPayPalService _payPalService;
    private readonly ILogger<PaymentService> _logger;

    /// <summary>
    /// Well-known product IDs. Must match the IDs configured in
    /// Google Play Console and App Store Connect.
    /// </summary>
    public static class Products
    {
        public const string TipSmall = "anticipack_tip_small";
        public const string TipMedium = "anticipack_tip_medium";
        public const string TipLarge = "anticipack_tip_large";

        public static readonly string[] AllTips = [TipSmall, TipMedium, TipLarge];
    }

    public event EventHandler<PurchaseResult>? PurchaseCompleted;

    public PaymentService(
        IStoreService storeService,
        IPayPalService payPalService,
        ILogger<PaymentService> logger)
    {
        _storeService = storeService;
        _payPalService = payPalService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PaymentMethod>> GetAvailableMethodsAsync()
    {
        var methods = new List<PaymentMethod>();

        if (await _storeService.IsAvailableAsync())
            methods.Add(PaymentMethod.StoreBilling);

        if (_payPalService.IsAvailable)
            methods.Add(PaymentMethod.PayPal);

        return methods;
    }

    public async Task<IReadOnlyList<ProductInfo>> GetProductsAsync()
    {
        try
        {
            return await _storeService.GetProductsAsync(Products.AllTips);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load products");
            return GetFallbackProducts();
        }
    }

    public async Task<PurchaseResult> PurchaseProductAsync(string productId, ProductType type)
    {
        _logger.LogInformation("Initiating store purchase for {ProductId}", productId);

        var result = await _storeService.PurchaseAsync(productId, type);

        if (result.Success)
        {
            _logger.LogInformation("Purchase completed: {ProductId}, Transaction: {TransactionId}",
                result.ProductId, result.TransactionId);
            PurchaseCompleted?.Invoke(this, result);
        }

        return result;
    }

    public async Task<PurchaseResult> ProcessPayPalPaymentAsync(decimal amount, string description = "")
    {
        _logger.LogInformation("Initiating PayPal payment for {Amount}", amount);

        var result = await _payPalService.ProcessPaymentAsync(amount, "USD", description);

        if (result.Success)
        {
            PurchaseCompleted?.Invoke(this, result);
        }

        return result;
    }

    public async Task<IReadOnlyList<PurchaseResult>> RestorePurchasesAsync()
    {
        _logger.LogInformation("Restoring purchases");
        return await _storeService.RestorePurchasesAsync();
    }

    /// <summary>
    /// Fallback product list when the store is unavailable.
    /// Prices shown are approximate; real prices come from the store.
    /// </summary>
    private static IReadOnlyList<ProductInfo> GetFallbackProducts() =>
    [
        new ProductInfo
        {
            ProductId = Products.TipSmall,
            Name = "Small Tip",
            LocalizedPrice = "$0.99",
            Description = "A small tip to support development",
            Type = ProductType.Consumable,
            IconClass = "fa fa-coffee"
        },
        new ProductInfo
        {
            ProductId = Products.TipMedium,
            Name = "Medium Tip",
            LocalizedPrice = "$2.99",
            Description = "A generous tip to support development",
            Type = ProductType.Consumable,
            IconClass = "fa fa-gift"
        },
        new ProductInfo
        {
            ProductId = Products.TipLarge,
            Name = "Large Tip",
            LocalizedPrice = "$4.99",
            Description = "An amazing tip to support development",
            Type = ProductType.Consumable,
            IconClass = "fa fa-heart"
        }
    ];
}
