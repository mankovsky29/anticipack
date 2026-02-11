using System.Net.Http.Json;
using System.Text.Json;
using Anticipack.Services.Sync.Dto;
using Microsoft.Extensions.Logging;

namespace Anticipack.Services.Sync;

/// <summary>
/// HTTP client for sync API operations.
/// </summary>
public class SyncApiClient : ISyncApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SyncApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SyncApiClient(HttpClient httpClient, ILogger<SyncApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<PremiumStatusDto> ValidatePremiumStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/subscription/status", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<PremiumStatusDto>(_jsonOptions, cancellationToken);
            return result ?? new PremiumStatusDto { IsPremium = false };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to validate premium status");
            throw;
        }
    }

    public async Task<SyncResponseDto> UploadDataAsync(SyncDataDto data, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/sync/upload", data, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<SyncResponseDto>(_jsonOptions, cancellationToken);
            return result ?? new SyncResponseDto { Success = false, ErrorMessage = "Invalid response" };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to upload data");
            return new SyncResponseDto 
            { 
                Success = false, 
                ErrorMessage = ex.Message,
                ErrorCode = "UPLOAD_FAILED"
            };
        }
    }

    public async Task<SyncDataDto?> DownloadDataAsync(DateTime? lastSyncTime = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = "api/sync/download";
            if (lastSyncTime.HasValue)
            {
                url += $"?since={lastSyncTime.Value:O}";
            }
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<SyncDataDto>(_jsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to download data");
            throw;
        }
    }

    public async Task<DateTime?> GetServerLastModifiedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/sync/last-modified", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (DateTime.TryParse(content.Trim('"'), out var lastModified))
            {
                return lastModified;
            }
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get server last modified time");
            return null;
        }
    }
}
