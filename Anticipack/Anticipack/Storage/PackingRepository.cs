using Anticipack.Storage.Repositories;
using SQLite;

namespace Anticipack.Storage
{
    /// <summary>
    /// Legacy repository implementation - maintained for backward compatibility.
    /// New code should use specific repository interfaces (IPackingActivityRepository, etc.)
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete - intentionally implementing legacy interface
    public sealed class PackingRepository : IPackingRepository
#pragma warning restore CS0618
    {
        private readonly SQLiteAsyncConnection _db;

        public PackingRepository(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitializeAsync()
        {
            await _db.CreateTablesAsync(CreateFlags.None, typeof(PackingItem), typeof(PackingActivity), typeof(PackingHistoryEntry));
        }

        // IPackingActivityRepository implementation
        public async Task<List<PackingActivity>> GetAllAsync()
        {
            return await _db.Table<PackingActivity>().OrderByDescending(x => x.LastPacked).ToListAsync();
        }

        public async Task<PackingActivity?> GetByIdAsync(string id)
        {
            return await _db.Table<PackingActivity>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddOrUpdateAsync(PackingActivity activity)
        {
            await _db.InsertOrReplaceAsync(activity);
        }

        public async Task DeleteAsync(string id)
        {
            var activity = await GetByIdAsync(id);
            if (activity is null)
                return;

            var items = await GetItemsForActivityAsync(id);
            foreach (var item in items)
            {
                await _db.DeleteAsync(item);
            }

            await DeleteHistoryForActivityAsync(id);
            await _db.DeleteAsync(activity);
        }

        public Task<string> CopyAsync(string activityId) => CopyPackingAsync(activityId);

        public async Task<string> CopyPackingAsync(string packingId)
        {
            var activity = await _db.Table<PackingActivity>().Where(a => a.Id == packingId).FirstAsync();

            var newActivity = new PackingActivity
            {
                Id = Guid.NewGuid().ToString(),
                Name = activity.Name + " (Copy)",
                LastPacked = DateTime.Now,
                RunCount = 0,
                IsRecurring = activity.IsRecurring,
                IsArchived = false,
                IsFinished = false,
                IsShared = false
            };

            var items = await _db.Table<PackingItem>()
                .Where(i => i.ActivityId == packingId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            foreach (var item in items)
            {
                var newItem = new PackingItem
                {
                    Id = Guid.NewGuid().ToString(),
                    ActivityId = newActivity.Id,
                    Name = item.Name,
                    Category = item.Category,
                    Notes = item.Notes,
                    SortOrder = item.SortOrder,
                    IsPacked = false
                };
                await _db.InsertAsync(newItem);
            }

            await _db.InsertAsync(newActivity);
            return newActivity.Id;
        }

        // IPackingItemRepository implementation
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

        // IPackingHistoryRepository implementation
        public async Task AddHistoryEntryAsync(PackingHistoryEntry entry)
        {
            await _db.InsertAsync(entry);
        }

        public async Task<List<PackingHistoryEntry>> GetHistoryForActivityAsync(string activityId, int? limit = null)
        {
            var query = _db.Table<PackingHistoryEntry>()
                .Where(x => x.ActivityId == activityId)
                .OrderByDescending(x => x.CompletedDate);

            if (limit.HasValue)
            {
                return await query.Take(limit.Value).ToListAsync();
            }

            return await query.ToListAsync();
        }

        public async Task DeleteHistoryForActivityAsync(string activityId)
        {
            var entries = await _db.Table<PackingHistoryEntry>()
                .Where(x => x.ActivityId == activityId)
                .ToListAsync();

            foreach (var entry in entries)
            {
                await _db.DeleteAsync(entry);
            }
        }
    }
}
