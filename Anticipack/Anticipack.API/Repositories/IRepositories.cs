using Anticipack.API.Models;

namespace Anticipack.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByExternalAuthIdAsync(string externalAuthId, AuthProvider provider);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(string id);
}

public interface IActivityRepository
{
    Task<PackingActivity?> GetByIdAsync(string id);
    Task<List<PackingActivity>> GetByUserIdAsync(string userId);
    Task<PackingActivity> CreateAsync(PackingActivity activity);
    Task<PackingActivity> UpdateAsync(PackingActivity activity);
    Task<bool> DeleteAsync(string id);
    Task<PackingActivity?> CopyActivityAsync(string activityId, string userId);
}

public interface ISettingsRepository
{
    Task<UserSettings?> GetByUserIdAsync(string userId);
    Task<UserSettings> CreateAsync(UserSettings settings);
    Task<UserSettings> UpdateAsync(UserSettings settings);
}

public interface IPackingItemRepository
{
    Task<PackingItem?> GetByIdAsync(string id);
    Task<List<PackingItem>> GetByActivityIdAsync(string activityId);
    Task<PackingItem> CreateAsync(PackingItem item);
    Task<PackingItem> UpdateAsync(PackingItem item);
    Task<bool> DeleteAsync(string id);
}
