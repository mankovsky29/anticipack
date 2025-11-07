# Mobile App Integration Guide

This guide helps you integrate your .NET MAUI app with the Anticipack API.

## Prerequisites

1. Install NuGet packages in your MAUI project:
```bash
dotnet add package System.Net.Http.Json
dotnet add package Google.Apis.Auth (for Google Sign-In)
dotnet add package Microsoft.Maui.Essentials.Interfaces
```

## Authentication Setup

### Google Sign-In Integration

1. Add the Google Sign-In package to your MAUI project
2. Configure Google Sign-In in your app

Example HttpClient service:

```csharp
public class AnticipaqApiService
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private string? _refreshToken;

    public AnticipaqApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://your-api-url.com")
        };
    }

    public async Task<bool> LoginWithGoogleAsync(string googleIdToken)
    {
        var request = new
        {
            idToken = googleIdToken,
            provider = "Google"
        };

        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            _accessToken = result.Data.AccessToken;
            _refreshToken = result.Data.RefreshToken;
            
            // Store tokens securely
            await SecureStorage.SetAsync("access_token", _accessToken);
            await SecureStorage.SetAsync("refresh_token", _refreshToken);
            
            return true;
        }
        
        return false;
    }

    private async Task<bool> RefreshTokenAsync()
    {
        _refreshToken = await SecureStorage.GetAsync("refresh_token");
        
        var request = new { refreshToken = _refreshToken };
        var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            _accessToken = result.Data.AccessToken;
            _refreshToken = result.Data.RefreshToken;
            
            await SecureStorage.SetAsync("access_token", _accessToken);
            await SecureStorage.SetAsync("refresh_token", _refreshToken);
            
            return true;
        }
        
        return false;
    }

    private async Task<HttpResponseMessage> SendAuthenticatedRequestAsync(
        HttpMethod method, string url, object? content = null)
    {
        _accessToken = await SecureStorage.GetAsync("access_token");
        
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        
        if (content != null)
        {
            request.Content = JsonContent.Create(content);
        }
        
        var response = await _httpClient.SendAsync(request);
        
        // If unauthorized, try refreshing token
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            if (await RefreshTokenAsync())
            {
                request.Headers.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                response = await _httpClient.SendAsync(request);
            }
        }
        
        return response;
    }

    // Activities
    public async Task<List<ActivityDto>?> GetActivitiesAsync()
    {
        var response = await SendAuthenticatedRequestAsync(HttpMethod.Get, "/api/activities");
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ActivityDto>>>();
            return result?.Data;
        }
        
        return null;
    }

    public async Task<ActivityDto?> CreateActivityAsync(string name, string? category, bool isShared)
    {
        var request = new { name, category, isShared };
        var response = await SendAuthenticatedRequestAsync(
            HttpMethod.Post, "/api/activities", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ActivityDto>>();
            return result?.Data;
        }
        
        return null;
    }

    public async Task<bool> UpdateActivityAsync(string activityId, string name, string? category, bool isShared)
    {
        var request = new { name, category, isShared };
        var response = await SendAuthenticatedRequestAsync(
            HttpMethod.Put, $"/api/activities/{activityId}", request);
        
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteActivityAsync(string activityId)
    {
        var response = await SendAuthenticatedRequestAsync(
            HttpMethod.Delete, $"/api/activities/{activityId}");
        
        return response.IsSuccessStatusCode;
    }

    public async Task<ActivityDto?> StartPackingAsync(string activityId)
    {
        var response = await SendAuthenticatedRequestAsync(
            HttpMethod.Post, $"/api/activities/{activityId}/start");
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ActivityDto>>();
            return result?.Data;
        }
        
        return null;
    }

    // Items
    public async Task<PackingItemDto?> CreateItemAsync(
        string activityId, string name, string? category, string? notes, int sortOrder)
    {
        var request = new { name, category, notes, sortOrder };
        var response = await SendAuthenticatedRequestAsync(
            HttpMethod.Post, $"/api/activities/{activityId}/items", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PackingItemDto>>();
            return result?.Data;
        }
        
        return null;
    }

    public async Task<bool> UpdateItemAsync(
        string activityId, string itemId, string name, bool isPacked, 
        string? category, string? notes, int sortOrder)
    {
        var request = new { name, isPacked, category, notes, sortOrder };
        var response = await SendAuthenticatedRequestAsync(
            HttpMethod.Put, $"/api/activities/{activityId}/items/{itemId}", request);
        
        return response.IsSuccessStatusCode;
    }

    // Settings
    public async Task<UserSettingsDto?> GetSettingsAsync()
    {
        var response = await SendAuthenticatedRequestAsync(HttpMethod.Get, "/api/settings");
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserSettingsDto>>();
            return result?.Data;
        }
        
        return null;
    }

    public async Task<bool> UpdateSettingsAsync(UpdateSettingsRequest settings)
    {
        var response = await SendAuthenticatedRequestAsync(
            HttpMethod.Put, "/api/settings", settings);
        
        return response.IsSuccessStatusCode;
    }
}

// DTOs (should match API DTOs)
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User,
    DateTime ExpiresAt
);

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message = null,
    List<string>? Errors = null
);

public record ActivityDto(
    string Id,
    string UserId,
    string Name,
    string? Category,
    DateTime LastPacked,
    int RunCount,
    bool IsShared,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<PackingItemDto> Items
);

public record PackingItemDto(
    string Id,
    string Name,
    bool IsPacked,
    string? Category,
    string? Notes,
    int SortOrder
);

public record UserSettingsDto(
    string Id,
    bool EnableNotifications,
    bool EnableEmailNotifications,
    bool EnablePushNotifications,
    string Theme,
    string DefaultCategory,
    bool AutoResetPackedItems,
    int ReminderHoursBeforePacking,
    bool AllowDataCollection,
    bool ShareAnonymousUsage,
    string Language,
    string DateFormat,
    bool ShowCompletedActivities,
    DateTime UpdatedAt
);

public record UpdateSettingsRequest(
    bool? EnableNotifications,
    bool? EnableEmailNotifications,
    bool? EnablePushNotifications,
    string? Theme,
    string? DefaultCategory,
    bool? AutoResetPackedItems,
    int? ReminderHoursBeforePacking,
    bool? AllowDataCollection,
    bool? ShareAnonymousUsage,
    string? Language,
    string? DateFormat,
    bool? ShowCompletedActivities
);

public record UserDto(
    string Id,
    string Email,
    string? DisplayName,
    string? ProfilePictureUrl,
    string AuthProvider,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
```

## Register Service in MauiProgram.cs

```csharp
builder.Services.AddSingleton<AnticipaqApiService>();
```

## Usage in ViewModels

```csharp
public class PackingActivitiesViewModel
{
    private readonly AnticipaqApiService _apiService;

    public PackingActivitiesViewModel(AnticipaqApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task LoadActivitiesAsync()
    {
        var activities = await _apiService.GetActivitiesAsync();
        // Update your UI
    }
}
```

## Testing Authentication

For development/testing without Google/Apple Sign-In:
1. Temporarily modify the API to accept a test token
2. Or implement email/password authentication
3. Or use the in-memory mock that returns a static JWT

## Production Considerations

1. **Secure Token Storage**: Use `SecureStorage.SetAsync()` for tokens
2. **Token Refresh**: Implement automatic token refresh before expiration
3. **Error Handling**: Add comprehensive error handling for network issues
4. **Offline Support**: Keep SQLite for offline mode, sync with API when online
5. **Retry Logic**: Use Polly for resilient HTTP calls

## Example Offline-First Sync

```csharp
public async Task SyncActivitiesAsync()
{
    // Upload local changes to API
    var localActivities = await _localRepository.GetPendingSyncAsync();
    foreach (var activity in localActivities)
    {
        await _apiService.CreateActivityAsync(activity.Name, activity.Category, activity.IsShared);
    }
    
    // Download remote changes
    var remoteActivities = await _apiService.GetActivitiesAsync();
    if (remoteActivities != null)
    {
        await _localRepository.UpdateFromRemoteAsync(remoteActivities);
    }
}
```
