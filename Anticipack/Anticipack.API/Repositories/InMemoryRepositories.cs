using Anticipack.API.Models;

namespace Anticipack.API.Repositories;

// In-Memory implementations for development
// TODO: Replace with actual database (Cosmos DB, SQL Server, etc.)

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public Task<User?> GetByIdAsync(string id)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(string email)
        => Task.FromResult(_users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByExternalAuthIdAsync(string externalAuthId, AuthProvider provider)
        => Task.FromResult(_users.FirstOrDefault(u => u.ExternalAuthId == externalAuthId && u.AuthProvider == provider));

    public Task<User> CreateAsync(User user)
    {
        _users.Add(user);
        return Task.FromResult(user);
    }

    public Task<User> UpdateAsync(User user)
    {
        var existing = _users.FirstOrDefault(u => u.Id == user.Id);
        if (existing != null)
        {
            _users.Remove(existing);
            _users.Add(user);
        }
        return Task.FromResult(user);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            _users.Remove(user);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}

public class InMemoryActivityRepository : IActivityRepository
{
    private readonly List<PackingActivity> _activities = new();

    public Task<PackingActivity?> GetByIdAsync(string id)
        => Task.FromResult(_activities.FirstOrDefault(a => a.Id == id));

    public Task<List<PackingActivity>> GetByUserIdAsync(string userId)
        => Task.FromResult(_activities.Where(a => a.UserId == userId).OrderByDescending(a => a.LastPacked).ToList());

    public Task<PackingActivity> CreateAsync(PackingActivity activity)
    {
        _activities.Add(activity);
        return Task.FromResult(activity);
    }

    public Task<PackingActivity> UpdateAsync(PackingActivity activity)
    {
        var existing = _activities.FirstOrDefault(a => a.Id == activity.Id);
        if (existing != null)
        {
            _activities.Remove(existing);
            _activities.Add(activity);
        }
        return Task.FromResult(activity);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var activity = _activities.FirstOrDefault(a => a.Id == id);
        if (activity != null)
        {
            _activities.Remove(activity);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<PackingActivity?> CopyActivityAsync(string activityId, string userId)
    {
        var original = _activities.FirstOrDefault(a => a.Id == activityId);
        if (original == null) return Task.FromResult<PackingActivity?>(null);

        var copy = new PackingActivity
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Name = $"{original.Name} (Copy)",
            IsShared = false,
            RunCount = 0,
            Items = original.Items.Select(item => new PackingItem
            {
                Id = Guid.NewGuid().ToString(),
                Name = item.Name,
                Category = item.Category,
                Notes = item.Notes,
                IsPacked = false,
                SortOrder = item.SortOrder
            }).ToList()
        };

        _activities.Add(copy);
        return Task.FromResult<PackingActivity?>(copy);
    }
}

public class InMemorySettingsRepository : ISettingsRepository
{
    private readonly List<UserSettings> _settings = new();

    public Task<UserSettings?> GetByUserIdAsync(string userId)
        => Task.FromResult(_settings.FirstOrDefault(s => s.UserId == userId));

    public Task<UserSettings> CreateAsync(UserSettings settings)
    {
        _settings.Add(settings);
        return Task.FromResult(settings);
    }

    public Task<UserSettings> UpdateAsync(UserSettings settings)
    {
        var existing = _settings.FirstOrDefault(s => s.Id == settings.Id);
        if (existing != null)
        {
            _settings.Remove(existing);
            _settings.Add(settings);
        }
        return Task.FromResult(settings);
    }
}

public class InMemoryPackingItemRepository : IPackingItemRepository
{
    private readonly List<PackingItem> _items = new();

    public Task<PackingItem?> GetByIdAsync(string id)
        => Task.FromResult(_items.FirstOrDefault(i => i.Id == id));

    public Task<List<PackingItem>> GetByActivityIdAsync(string activityId)
        => Task.FromResult(_items.Where(i => i.ActivityId == activityId).OrderBy(i => i.SortOrder).ToList());

    public Task<PackingItem> CreateAsync(PackingItem item)
    {
        _items.Add(item);
        return Task.FromResult(item);
    }

    public Task<PackingItem> UpdateAsync(PackingItem item)
    {
        var existing = _items.FirstOrDefault(i => i.Id == item.Id);
        if (existing != null)
        {
            _items.Remove(existing);
            _items.Add(item);
        }
        return Task.FromResult(item);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item != null)
        {
            _items.Remove(item);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
