using System.Collections.Concurrent;

namespace Anticipack.Workers.Services;

public interface IUserSessionManager
{
    Task<string?> GetOrCreateTokenAsync(long telegramUserId, string? firstName, string? lastName, string? username);
    void InvalidateSession(long telegramUserId);
    string? GetCurrentActivityId(long telegramUserId);
    void SetCurrentActivity(long telegramUserId, string activityId);
    void ClearCurrentActivity(long telegramUserId);
}

public class UserSessionManager : IUserSessionManager
{
    private readonly ConcurrentDictionary<long, UserSession> _sessions = new();
    private readonly IAnticipackApiClient _apiClient;
    private readonly ILogger<UserSessionManager> _logger;

    public UserSessionManager(IAnticipackApiClient apiClient, ILogger<UserSessionManager> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<string?> GetOrCreateTokenAsync(long telegramUserId, string? firstName, string? lastName, string? username)
    {
        if (_sessions.TryGetValue(telegramUserId, out var session) && session.ExpiresAt > DateTime.UtcNow)
        {
            return session.Token;
        }

        var token = await _apiClient.AuthenticateTelegramUserAsync(telegramUserId, firstName, lastName, username);
        if (token == null)
        {
            _logger.LogWarning("Failed to authenticate Telegram user {UserId}", telegramUserId);
            return null;
        }

        var newSession = new UserSession
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(23),
            CurrentActivityId = session?.CurrentActivityId
        };

        _sessions[telegramUserId] = newSession;
        return token;
    }

    public void InvalidateSession(long telegramUserId)
    {
        _sessions.TryRemove(telegramUserId, out _);
    }

    public string? GetCurrentActivityId(long telegramUserId)
    {
        return _sessions.TryGetValue(telegramUserId, out var session) ? session.CurrentActivityId : null;
    }

    public void SetCurrentActivity(long telegramUserId, string activityId)
    {
        if (_sessions.TryGetValue(telegramUserId, out var session))
        {
            session.CurrentActivityId = activityId;
        }
    }

    public void ClearCurrentActivity(long telegramUserId)
    {
        if (_sessions.TryGetValue(telegramUserId, out var session))
        {
            session.CurrentActivityId = null;
        }
    }

    private class UserSession
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string? CurrentActivityId { get; set; }
    }
}
