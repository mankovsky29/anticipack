
namespace Anticipack.Storage
{
    internal interface IPackingRepository
    {
        Task InitializeAsync();
        Task<List<PackingActivity>> GetAllAsync();
        Task<PackingActivity?> GetByIdAsync(string id);
        Task AddOrUpdateAsync(PackingActivity packing);
        Task DeleteAsync(string id);

        Task<List<PackingItem>> GetItemsForActivityAsync(string activityId);

        Task AddItemToActivityAsync(string activityId, PackingItem item);
        Task DeleteItemAsync(string itemId);
    }
}