using SQLite;

namespace Anticipack.Storage.Repositories;

/// <summary>
/// SQLite implementation of IPackingItemRepository
/// </summary>
public sealed class PackingItemRepository : IPackingItemRepository
{
    private readonly SQLiteAsyncConnection _db;

    public PackingItemRepository(IDatabaseConnectionFactory connectionFactory)
    {
        _db = connectionFactory.CreateConnection();
    }

    public async Task<List<PackingItem>> GetItemsForActivityAsync(string activityId)
    {
        return await _db.Table<PackingItem>()
            .Where(x => x.ActivityId == activityId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
    }

    public async Task AddItemToActivityAsync(string activityId, PackingItem item)
    {
        item.ActivityId = activityId;

        var existingItems = await _db.Table<PackingItem>()
            .Where(x => x.ActivityId == activityId)
            .ToListAsync();

        item.SortOrder = existingItems.Count > 0 ? existingItems.Max(x => x.SortOrder) + 1 : 0;

        await _db.InsertAsync(item);
    }

    public async Task AddOrUpdateItemAsync(PackingItem item)
    {
        await _db.InsertOrReplaceAsync(item);
    }

    public async Task UpdateItemsSortOrderAsync(IEnumerable<PackingItem> items)
    {
        await _db.RunInTransactionAsync(connection =>
        {
            foreach (var item in items)
            {
                connection.Update(item);
            }
        });
    }

    public async Task DeleteItemAsync(string itemId)
    {
        await _db.DeleteAsync(new PackingItem { Id = itemId });
    }
}
