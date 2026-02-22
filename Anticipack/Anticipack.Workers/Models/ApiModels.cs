namespace Anticipack.Workers.Models;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message = null,
    List<string>? Errors = null
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User,
    DateTime ExpiresAt
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

public record TelegramLoginRequest(
    long TelegramUserId,
    string? FirstName,
    string? LastName,
    string? Username
);

public record CreateActivityRequest(
    string Name,
    bool IsShared = false,
    bool IsRecurring = true
);

public record CreatePackingItemRequest(
    string Name,
    string? Category,
    string? Notes,
    int SortOrder = 0
);
