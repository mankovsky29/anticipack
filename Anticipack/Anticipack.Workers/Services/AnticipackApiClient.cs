using System.Net.Http.Headers;
using System.Net.Http.Json;
using Anticipack.Workers.Models;

namespace Anticipack.Workers.Services;

public interface IAnticipackApiClient
{
    Task<string?> AuthenticateTelegramUserAsync(long telegramUserId, string? firstName, string? lastName, string? username);
    Task<List<ActivityDto>> GetActivitiesAsync(string token);
    Task<ActivityDto?> GetActivityAsync(string token, string activityId);
    Task<ActivityDto?> CreateActivityAsync(string token, string name);
    Task<bool> DeleteActivityAsync(string token, string activityId);
    Task<PackingItemDto?> AddItemAsync(string token, string activityId, string name, string? category = null);
    Task<PackingItemDto?> ToggleItemAsync(string token, string activityId, string itemId);
    Task<bool> DeleteItemAsync(string token, string activityId, string itemId);
}

public class AnticipackApiClient : IAnticipackApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _botApiKey;
    private readonly ILogger<AnticipackApiClient> _logger;

    public AnticipackApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<AnticipackApiClient> logger)
    {
        _httpClient = httpClient;
        _botApiKey = configuration["Api:BotApiKey"] ?? "";
        _logger = logger;
    }

    public async Task<string?> AuthenticateTelegramUserAsync(long telegramUserId, string? firstName, string? lastName, string? username)
    {
        try
        {
            var request = new TelegramLoginRequest(telegramUserId, firstName, lastName, username);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/auth/telegram");
            httpRequest.Headers.Add("X-Bot-Api-Key", _botApiKey);
            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Telegram auth failed with status {Status}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
            return result?.Data?.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating Telegram user {UserId}", telegramUserId);
            return null;
        }
    }

    public async Task<List<ActivityDto>> GetActivitiesAsync(string token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/activities");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return [];

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ActivityDto>>>();
            return result?.Data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activities");
            return [];
        }
    }

    public async Task<ActivityDto?> GetActivityAsync(string token, string activityId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/activities/{activityId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ActivityDto>>();
            return result?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity {ActivityId}", activityId);
            return null;
        }
    }

    public async Task<ActivityDto?> CreateActivityAsync(string token, string name)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/activities");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new CreateActivityRequest(name));

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ActivityDto>>();
            return result?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating activity");
            return null;
        }
    }

    public async Task<bool> DeleteActivityAsync(string token, string activityId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/activities/{activityId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting activity {ActivityId}", activityId);
            return false;
        }
    }

    public async Task<PackingItemDto?> AddItemAsync(string token, string activityId, string name, string? category = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/activities/{activityId}/items");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new CreatePackingItemRequest(name, category, null));

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PackingItemDto>>();
            return result?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to activity {ActivityId}", activityId);
            return null;
        }
    }

    public async Task<PackingItemDto?> ToggleItemAsync(string token, string activityId, string itemId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/activities/{activityId}/items/{itemId}/toggle");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PackingItemDto>>();
            return result?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling item {ItemId}", itemId);
            return null;
        }
    }

    public async Task<bool> DeleteItemAsync(string token, string activityId, string itemId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/activities/{activityId}/items/{itemId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item {ItemId}", itemId);
            return false;
        }
    }
}
