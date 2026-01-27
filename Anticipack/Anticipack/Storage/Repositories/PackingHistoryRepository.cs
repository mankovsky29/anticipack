using SQLite;

namespace Anticipack.Storage.Repositories;

/// <summary>
/// SQLite implementation of IPackingHistoryRepository
/// </summary>
public sealed class PackingHistoryRepository : IPackingHistoryRepository
{
    private readonly SQLiteAsyncConnection _db;

    public PackingHistoryRepository(IDatabaseConnectionFactory connectionFactory)
    {
        _db = connectionFactory.CreateConnection();
    }

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
