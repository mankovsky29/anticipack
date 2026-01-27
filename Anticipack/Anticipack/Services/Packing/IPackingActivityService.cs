using Anticipack.Storage;

namespace Anticipack.Services.Packing;

/// <summary>
/// Service for packing activity business operations (SRP: Business logic separated from UI)
/// </summary>
public interface IPackingActivityService
{
    Task<PackingActivity> GetOrCreateActivityAsync(string? id);
    Task<List<PackingItem>> GetItemsAsync(string activityId);
    Task SaveActivityAsync(PackingActivity activity);
    Task<string> CopyActivityAsync(string activityId);
    Task DeleteActivityAsync(string activityId);
    Task AddItemAsync(string activityId, string name, string category, string? notes = null);
    Task AddItemsAsync(string activityId, IEnumerable<string> itemNames, string category);
    Task UpdateItemAsync(PackingItem item);
    Task DeleteItemAsync(string itemId);
    Task UpdateItemsOrderAsync(IEnumerable<PackingItem> items);
}
