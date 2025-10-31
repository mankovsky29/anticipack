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
            //await _db.CreateTableAsync<PackingItem>();
            //await _db.CreateTableAsync<PackingActivity>();
            await _db.CreateTablesAsync(CreateFlags.None, typeof(PackingItem), typeof(PackingActivity));
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
            {
                var items = await GetItemsForActivityAsync(id);
                foreach (var item in items)
                {
                    await _db.DeleteAsync(item);
                }

                await _db.DeleteAsync(packing);
            }
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

        public async Task<string> CopyPackingAsync(string packingId)
        {
            var packingActivity = await _db.Table<PackingActivity>().Where(packing => packingId == packing.Id).FirstAsync();

            var newPackingActivity = new PackingActivity
            {
                Id = Guid.NewGuid().ToString(),
                Name = packingActivity.Name + " (Copy)",
                LastPacked = DateTime.Now,
                Category = packingActivity.Category,
                RunCount = 0
            };

            var packingItems = await _db.Table<PackingItem>().Where(packing => packingId == packing.ActivityId).ToListAsync();

            foreach (var item in packingItems)
            {
                //var newItem = new PackingItem
                //{
                //    Id = Guid.NewGuid().ToString(),
                //    ActivityId = newPackingActivity.Id,
                //    Name = item.Name,
                //    IsPacked = false,
                //    Category = item.Category,
                //    Notes = item.Notes
                //};
                item.Id = Guid.NewGuid().ToString();
                item.ActivityId = newPackingActivity.Id;

                await _db.InsertAsync(item);
            }

            await _db.InsertAsync(newPackingActivity);
            return newPackingActivity.Id.ToString();
        }

        public Task AddOrUpdateItemAsync(PackingItem item)
        {
            return _db.InsertOrReplaceAsync(item);
        }
    }
}
