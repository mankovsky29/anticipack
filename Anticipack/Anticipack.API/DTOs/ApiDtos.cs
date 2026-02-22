namespace Anticipack.API.DTOs;

// Auth DTOs
public record LoginRequest(string IdToken, string Provider); // Google or Apple ID token

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User,
    DateTime ExpiresAt
);

public record RefreshTokenRequest(string RefreshToken);

// User DTOs
public record UserDto(
    string Id,
    string Email,
    string? DisplayName,
    string? ProfilePictureUrl,
    string AuthProvider,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record UpdateUserRequest(
    string? DisplayName,
    string? ProfilePictureUrl
);

// Activity DTOs
public record CreateActivityRequest(
    string Name,
    bool IsShared = false,
    bool IsRecurring = true
);

public record UpdateActivityRequest(
    string Name,
    bool IsShared,
    bool IsRecurring = true
);

public record ActivityDto(
    string Id,
    string UserId,
    string Name,
    DateTime LastPacked,
    int RunCount,
    bool IsShared,
    bool IsArchived,
    bool IsFinished,
    bool IsRecurring,
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

public record CreatePackingItemRequest(
    string Name,
    string? Category,
    string? Notes,
    int SortOrder = 0
);

public record UpdatePackingItemRequest(
    string Name,
    bool IsPacked,
    string? Category,
    string? Notes,
    int SortOrder
);

// Settings DTOs
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

// Common
public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message = null,
    List<string>? Errors = null
);

// History DTOs
public record PackingHistoryEntryDto(
    string Id,
    string ActivityId,
    DateTime CompletedDate,
    int TotalItems,
    int PackedItems,
    int DurationSeconds,
    DateTime StartTime,
    DateTime EndTime
);

public record CreateHistoryEntryRequest(
    int TotalItems,
    int PackedItems,
    int DurationSeconds,
    DateTime StartTime,
    DateTime EndTime
);

// Telegram Auth
public record TelegramLoginRequest(
    long TelegramUserId,
    string? FirstName,
    string? LastName,
    string? Username
);
