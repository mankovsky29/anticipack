namespace Anticipack.Services.Payment;

/// <summary>
/// Represents the outcome of a purchase attempt.
/// </summary>
public class PurchaseResult
{
    /// <summary>
    /// Whether the purchase completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The product that was purchased.
    /// </summary>
    public string ProductId { get; init; } = string.Empty;

    /// <summary>
    /// Transaction identifier from the store (for receipt validation).
    /// </summary>
    public string? TransactionId { get; init; }

    /// <summary>
    /// Payment method used for the purchase.
    /// </summary>
    public PaymentMethod Method { get; init; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Error details if the purchase failed.
    /// </summary>
    public string? ErrorDetail { get; init; }

    /// <summary>
    /// Whether the user cancelled the purchase themselves.
    /// </summary>
    public bool WasCancelled { get; init; }

    public static PurchaseResult Succeeded(string productId, string transactionId, PaymentMethod method) =>
        new()
        {
            Success = true,
            ProductId = productId,
            TransactionId = transactionId,
            Method = method,
            Message = "Purchase completed successfully."
        };

    public static PurchaseResult Failed(string productId, string message, string? errorDetail = null) =>
        new()
        {
            Success = false,
            ProductId = productId,
            Message = message,
            ErrorDetail = errorDetail
        };

    public static PurchaseResult Cancelled(string productId) =>
        new()
        {
            Success = false,
            ProductId = productId,
            WasCancelled = true,
            Message = "Purchase was cancelled."
        };
}
