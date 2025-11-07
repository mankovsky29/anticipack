using System.Security.Claims;

namespace Anticipack.API.Services;

public interface IAuthService
{
    Task<(bool Success, string? UserId, string? Email, string? Name, string? Picture)> ValidateGoogleTokenAsync(string idToken);
    Task<(bool Success, string? UserId, string? Email, string? Name)> ValidateAppleTokenAsync(string idToken);
    string GenerateJwtToken(string userId, string email);
    string GenerateRefreshToken();
    Task<string?> ValidateRefreshTokenAsync(string refreshToken);
    ClaimsPrincipal? ValidateJwtToken(string token);
}
