namespace Anticipack.Storage.Repositories;

/// <summary>
/// Repository interface for PackingActivity CRUD operations (SRP: Single responsibility for activity persistence)
/// </summary>
public interface IPackingActivityRepository
{
    Task InitializeAsync();
    Task<List<PackingActivity>> GetAllAsync();
    Task<PackingActivity?> GetByIdAsync(string id);
    Task AddOrUpdateAsync(PackingActivity activity);
    Task DeleteAsync(string id);
    Task<string> CopyAsync(string activityId);
}
