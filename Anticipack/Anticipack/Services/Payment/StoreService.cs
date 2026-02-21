using Microsoft.Extensions.Logging;
using Plugin.InAppBilling;

namespace Anticipack.Services.Payment;

/// <summary>
/// Wraps Plugin.InAppBilling for native in-app purchases (SRP).
/// Handles Google Play Billing and Apple StoreKit, which natively support
/// Google Pay and Apple Pay as payment methods within their checkout flows.
/// </summary>
public class StoreService : IStoreService
{
    private readonly ILogger<StoreService> _logger;

    public StoreService(ILogger<StoreService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var connected = await CrossInAppBilling.Current.ConnectAsync();
            if (connected)
                await CrossInAppBilling.Current.DisconnectAsync();
            return connected;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Store billing is not available");
            return false;
        }
    }

    public async Task<IReadOnlyList<ProductInfo>> GetProductsAsync(IEnumerable<string> productIds)
    {
        var products = new List<ProductInfo>();

        try
        {
            var connected = await CrossInAppBilling.Current.ConnectAsync();
            if (!connected)
            {
                _logger.LogWarning("Could not connect to store billing service");
                return products;
            }

            var items = await CrossInAppBilling.Current.GetProductInfoAsync(
                ItemType.InAppPurchaseConsumable, productIds.ToArray());

            if (items is not null)
            {
                foreach (var item in items)
                {
                    products.Add(new ProductInfo
                    {
                        ProductId = item.ProductId,
                        Name = item.Name,
                        LocalizedPrice = item.LocalizedPrice,
                        PriceMicros = item.MicrosPrice,
                        CurrencyCode = item.CurrencyCode,
                        Description = item.Description,
                        Type = ProductType.Consumable
                    });
                }
            }
        }
        catch (InAppBillingPurchaseException ex)
        {
            _logger.LogError(ex, "Failed to retrieve products from store");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving products");
        }
        finally
        {
            await DisconnectSafely();
        }

        return products;
    }

    public async Task<PurchaseResult> PurchaseAsync(string productId, ProductType type)
    {
        try
        {
            var connected = await CrossInAppBilling.Current.ConnectAsync();
            if (!connected)
                return PurchaseResult.Failed(productId, "Could not connect to the store.");

            var itemType = type switch
            {
                ProductType.Consumable => ItemType.InAppPurchaseConsumable,
                ProductType.NonConsumable => ItemType.InAppPurchase,
                ProductType.Subscription => ItemType.Subscription,
                _ => ItemType.InAppPurchaseConsumable
            };

            var purchase = await CrossInAppBilling.Current.PurchaseAsync(productId, itemType);

            if (purchase is null)
                return PurchaseResult.Cancelled(productId);

            // For consumables, acknowledge/consume the purchase
            if (type == ProductType.Consumable)
            {
                await CrossInAppBilling.Current.ConsumePurchaseAsync(
                    purchase.ProductId, purchase.PurchaseToken);
            }

            return PurchaseResult.Succeeded(
                productId,
                purchase.Id,
                PaymentMethod.StoreBilling);
        }
        catch (InAppBillingPurchaseException ex) when (ex.PurchaseError == PurchaseError.UserCancelled)
        {
            return PurchaseResult.Cancelled(productId);
        }
        catch (InAppBillingPurchaseException ex)
        {
            _logger.LogError(ex, "Store purchase failed for {ProductId}: {Error}", productId, ex.PurchaseError);
            return PurchaseResult.Failed(productId, "Purchase failed.", ex.PurchaseError.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during purchase of {ProductId}", productId);
            return PurchaseResult.Failed(productId, "An unexpected error occurred.");
        }
        finally
        {
            await DisconnectSafely();
        }
    }

    public async Task<IReadOnlyList<PurchaseResult>> RestorePurchasesAsync()
    {
        var results = new List<PurchaseResult>();

        try
        {
            var connected = await CrossInAppBilling.Current.ConnectAsync();
            if (!connected)
                return results;

            // Restore non-consumable and subscription purchases
            var purchases = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.InAppPurchase);
            if (purchases is not null)
            {
                foreach (var p in purchases)
                {
                    results.Add(PurchaseResult.Succeeded(p.ProductId, p.Id, PaymentMethod.StoreBilling));
                }
            }

            var subscriptions = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.Subscription);
            if (subscriptions is not null)
            {
                foreach (var s in subscriptions)
                {
                    results.Add(PurchaseResult.Succeeded(s.ProductId, s.Id, PaymentMethod.StoreBilling));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore purchases");
        }
        finally
        {
            await DisconnectSafely();
        }

        return results;
    }

    private async Task DisconnectSafely()
    {
        try
        {
            await CrossInAppBilling.Current.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disconnecting from store billing");
        }
    }
}
