using Anticipack.Storage.Repositories;

namespace Anticipack.Storage
{
    /// <summary>
    /// Legacy facade interface that combines all repository operations.
    /// Maintained for backward compatibility - prefer using specific repository interfaces.
    /// </summary>
    [Obsolete("Use IPackingActivityRepository, IPackingItemRepository, or IPackingHistoryRepository instead")]
    public interface IPackingRepository : IPackingActivityRepository, IPackingItemRepository, IPackingHistoryRepository
    {
        // Legacy method - use CopyAsync from IPackingActivityRepository
        Task<string> CopyPackingAsync(string packingId);
    }
}