using Microsoft.Extensions.Logging;

namespace Anticipack.Services.Sync;

/// <summary>
/// Manages premium subscription status with caching to minimize API calls.
/// </summary>
public class PremiumService : IPremiumService
{
    private readonly IPremiumApiClient _apiClient;
    private readonly ILogger<PremiumService> _logger;

    private bool? _cachedPremiumStatus;
    private DateTime? _cachedExpirationDate;
    private DateTime _lastChecked = DateTime.MinValue;

    // Cache duration - avoid frequent API calls
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    // Preferences keys for persistent storage
    private const string PremiumStatusKey = "premium_status";
    private const string PremiumExpirationKey = "premium_expiration";
    private const string LastCheckKey = "premium_last_check";

    public event EventHandler<PremiumStatusChangedEventArgs>? PremiumStatusChanged;

    public PremiumService(IPremiumApiClient apiClient, ILogger<PremiumService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        LoadCachedStatus();
    }

    public async Task<bool> IsPremiumAsync()
    {
        // Return cached value if still valid
        if (_cachedPremiumStatus.HasValue && DateTime.UtcNow - _lastChecked < CacheDuration)
        {
            // Also check if subscription hasn't expired
            if (_cachedExpirationDate.HasValue && _cachedExpirationDate.Value < DateTime.UtcNow)
            {
                _cachedPremiumStatus = false;
                SaveCachedStatus();
                return false;
            }
            return _cachedPremiumStatus.Value;
        }

        return await RefreshPremiumStatusAsync();
    }

    public async Task<bool> RefreshPremiumStatusAsync()
    {
        try
        {
            var status = await _apiClient.ValidatePremiumStatusAsync();

            var previousStatus = _cachedPremiumStatus;
            _cachedPremiumStatus = status.IsPremium;
            _cachedExpirationDate = status.ExpirationDate;
            _lastChecked = DateTime.UtcNow;

            SaveCachedStatus();

            // Raise event if status changed
            if (previousStatus != _cachedPremiumStatus)
            {
                PremiumStatusChanged?.Invoke(this,
                    new PremiumStatusChangedEventArgs(_cachedPremiumStatus.Value, _cachedExpirationDate));
            }

            return _cachedPremiumStatus.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh premium status, using cached value");
            return _cachedPremiumStatus ?? false;
        }
    }

    public async Task<DateTime?> GetSubscriptionExpirationAsync()
    {
        if (_cachedExpirationDate.HasValue && DateTime.UtcNow - _lastChecked < CacheDuration)
        {
            return _cachedExpirationDate;
        }

        await RefreshPremiumStatusAsync();
        return _cachedExpirationDate;
    }

    private void LoadCachedStatus()
    {
        try
        {
            if (Preferences.ContainsKey(PremiumStatusKey))
            {
                _cachedPremiumStatus = Preferences.Get(PremiumStatusKey, false);
            }

            if (Preferences.ContainsKey(PremiumExpirationKey))
            {
                var expTicks = Preferences.Get(PremiumExpirationKey, 0L);
                if (expTicks > 0)
                {
                    _cachedExpirationDate = new DateTime(expTicks, DateTimeKind.Utc);
                }
            }

            if (Preferences.ContainsKey(LastCheckKey))
            {
                var lastCheckTicks = Preferences.Get(LastCheckKey, 0L);
                if (lastCheckTicks > 0)
                {
                    _lastChecked = new DateTime(lastCheckTicks, DateTimeKind.Utc);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cached premium status");
        }
    }

    private void SaveCachedStatus()
    {
        try
        {
            Preferences.Set(PremiumStatusKey, _cachedPremiumStatus ?? false);
            Preferences.Set(PremiumExpirationKey, _cachedExpirationDate?.Ticks ?? 0L);
            Preferences.Set(LastCheckKey, _lastChecked.Ticks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save cached premium status");
        }
    }
}
