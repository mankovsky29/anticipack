using Anticipack.Storage;
using Anticipack.Storage.Repositories;

namespace Anticipack.Services.Packing;

/// <summary>
/// Implementation of packing activity business operations
/// </summary>
public sealed class PackingActivityService : IPackingActivityService
{
    private readonly IPackingActivityRepository _activityRepository;
    private readonly IPackingItemRepository _itemRepository;

    public PackingActivityService(
        IPackingActivityRepository activityRepository,
        IPackingItemRepository itemRepository)
    {
        _activityRepository = activityRepository;
        _itemRepository = itemRepository;
    }

    public async Task<PackingActivity> GetOrCreateActivityAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new PackingActivity();
        }

        return await _activityRepository.GetByIdAsync(id) ?? new PackingActivity();
    }

    public async Task<List<PackingItem>> GetItemsAsync(string activityId)
    {
        return await _itemRepository.GetItemsForActivityAsync(activityId);
    }

    public async Task SaveActivityAsync(PackingActivity activity)
    {
        await _activityRepository.AddOrUpdateAsync(activity);
    }

    public async Task<string> CopyActivityAsync(string activityId)
    {
        return await _activityRepository.CopyAsync(activityId);
    }

    public async Task DeleteActivityAsync(string activityId)
    {
        await _activityRepository.DeleteAsync(activityId);
    }

    public async Task AddItemAsync(string activityId, string name, string category, string? notes = null)
    {
        var item = new PackingItem
        {
            Name = name.Trim(),
            Category = category,
            Notes = notes ?? string.Empty,
            ActivityId = activityId
        };

        await _itemRepository.AddItemToActivityAsync(activityId, item);
    }

    public async Task AddItemsAsync(string activityId, IEnumerable<string> itemNames, string category)
    {
        foreach (var name in itemNames)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await AddItemAsync(activityId, name, category);
            }
        }
    }

    public async Task UpdateItemAsync(PackingItem item)
    {
        await _itemRepository.AddOrUpdateItemAsync(item);
    }

    public async Task DeleteItemAsync(string itemId)
    {
        await _itemRepository.DeleteItemAsync(itemId);
    }

    public async Task UpdateItemsOrderAsync(IEnumerable<PackingItem> items)
    {
        await _itemRepository.UpdateItemsSortOrderAsync(items);
    }
}
