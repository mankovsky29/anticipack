namespace Anticipack.Storage.Repositories;

/// <summary>
/// Repository interface for PackingHistoryEntry CRUD operations (SRP: Single responsibility for history persistence)
/// </summary>
public interface IPackingHistoryRepository
{
    Task AddHistoryEntryAsync(PackingHistoryEntry entry);
    Task<List<PackingHistoryEntry>> GetHistoryForActivityAsync(string activityId, int? limit = null);
    Task DeleteHistoryForActivityAsync(string activityId);
}
