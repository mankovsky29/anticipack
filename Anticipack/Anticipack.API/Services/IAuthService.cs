using Anticipack.API.Models;
using System.Security.Claims;

namespace Anticipack.API.Services;

public interface IAuthService
{
    string GenerateJwtToken(string userId, string email, string? deviceId = null);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    Task<RefreshToken> CreateRefreshTokenAsync(string userId, string refreshToken, DateTime expiresAtUtc, string? createdByIp);
    Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, string? revokedByIp);
    Task<(RefreshToken RevokedToken, RefreshToken NewToken)?> RotateRefreshTokenAsync(
        string refreshToken,
        string userId,
        DateTime newExpiryDateUtc,
        string? requestIp);
    ClaimsPrincipal? ValidateJwtToken(string token);
}
