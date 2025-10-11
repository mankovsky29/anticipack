using SQLite;

namespace Anticipack.Storage
{
    internal class PackingRepository : IPackingRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public PackingRepository(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitializeAsync()
        {
            await _db.CreateTableAsync<PackingItem>();
            await _db.CreateTableAsync<PackingActivity>();
        }

        public async Task<List<PackingActivity>> GetAllAsync()
        {
            return await _db.Table<PackingActivity>().OrderByDescending(x => x.LastPacked).ToListAsync();
        }

        public async Task<PackingActivity?> GetByIdAsync(string id)
        {
            return await _db.Table<PackingActivity>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddOrUpdateAsync(PackingActivity packing)
        {
            await _db.InsertOrReplaceAsync(packing);
        }

        public async Task DeleteAsync(string id)
        {
            var packing = await GetByIdAsync(id);
            if (packing != null)
                await _db.DeleteAsync(packing);
        }

        public async Task<List<PackingItem>> GetItemsForActivityAsync(string activityId)
        {
            return await _db.Table<PackingItem>().Where(x => x.ActivityId == activityId).ToListAsync();
        }

        public async Task AddItemToActivityAsync(string activityId, PackingItem item)
        {
            item.ActivityId = activityId;
            await _db.InsertAsync(item);
        }

        public Task DeleteItemAsync(string itemId)
        {
            return _db.DeleteAsync(new PackingItem { Id = itemId });
        }
    }
}
