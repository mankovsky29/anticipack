using SQLite;

namespace Anticipack.Storage.Repositories;

/// <summary>
/// SQLite implementation of IPackingActivityRepository
/// </summary>
public sealed class PackingActivityRepository : IPackingActivityRepository
{
    private readonly SQLiteAsyncConnection _db;

    public PackingActivityRepository(IDatabaseConnectionFactory connectionFactory)
    {
        _db = connectionFactory.CreateConnection();
    }

    public async Task InitializeAsync()
    {
        await _db.CreateTablesAsync(CreateFlags.None, 
            typeof(PackingItem), 
            typeof(PackingActivity), 
            typeof(PackingHistoryEntry));
    }

    public async Task<List<PackingActivity>> GetAllAsync()
    {
        return await _db.Table<PackingActivity>()
            .OrderByDescending(x => x.LastPacked)
            .ToListAsync();
    }

    public async Task<PackingActivity?> GetByIdAsync(string id)
    {
        return await _db.Table<PackingActivity>()
            .FirstOrDefaultAsync(x => x.Id == id);
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

        // Delete related items directly
        var items = await _db.Table<PackingItem>()
            .Where(x => x.ActivityId == id)
            .ToListAsync();
        foreach (var item in items)
        {
            await _db.DeleteAsync(item);
        }

        // Delete related history directly
        var historyEntries = await _db.Table<PackingHistoryEntry>()
            .Where(x => x.ActivityId == id)
            .ToListAsync();
        foreach (var entry in historyEntries)
        {
            await _db.DeleteAsync(entry);
        }

        await _db.DeleteAsync(activity);
    }

    public async Task<string> CopyAsync(string activityId)
    {
        var activity = await _db.Table<PackingActivity>()
            .Where(a => a.Id == activityId)
            .FirstAsync();

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
            .Where(x => x.ActivityId == activityId)
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
}
