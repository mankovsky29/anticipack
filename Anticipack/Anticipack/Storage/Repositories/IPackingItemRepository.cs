namespace Anticipack.Storage.Repositories;

/// <summary>
/// Repository interface for PackingItem CRUD operations (SRP: Single responsibility for item persistence)
/// </summary>
public interface IPackingItemRepository
{
    Task<List<PackingItem>> GetItemsForActivityAsync(string activityId);
    Task AddItemToActivityAsync(string activityId, PackingItem item);
    Task AddOrUpdateItemAsync(PackingItem item);
    Task UpdateItemsSortOrderAsync(IEnumerable<PackingItem> items);
    Task DeleteItemAsync(string itemId);
}
