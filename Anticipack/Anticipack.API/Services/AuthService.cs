using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Anticipack.API.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, (string UserId, DateTime ExpiresAt)> _refreshTokens = new();

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<(bool Success, string? UserId, string? Email, string? Name, string? Picture)> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Authentication:Google:ClientId"] ?? "" }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            
            return (true, payload.Subject, payload.Email, payload.Name, payload.Picture);
        }
        catch (Exception)
        {
            return (false, null, null, null, null);
        }
    }

    public async Task<(bool Success, string? UserId, string? Email, string? Name)> ValidateAppleTokenAsync(string idToken)
    {
        try
        {
            // Apple Sign In validation requires fetching Apple's public keys and validating the JWT
            // This is a simplified version - in production, implement full Apple ID token validation
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(idToken);
            
            var userId = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

            // TODO: Add proper Apple public key validation
            // For now, we'll accept the token (NOT PRODUCTION READY)
            
            return await Task.FromResult((true, userId, email, name));
        }
        catch (Exception)
        {
            return (false, null, null, null);
        }
    }

    public string GenerateJwtToken(string userId, string email)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "your-secret-key-min-32-characters-long-for-security"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "anticipack-api",
            audience: _configuration["Jwt:Audience"] ?? "anticipack-app",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public Task<string?> ValidateRefreshTokenAsync(string refreshToken)
    {
        if (_refreshTokens.TryGetValue(refreshToken, out var tokenData))
        {
            if (tokenData.ExpiresAt > DateTime.UtcNow)
            {
                return Task.FromResult<string?>(tokenData.UserId);
            }
            _refreshTokens.Remove(refreshToken);
        }
        return Task.FromResult<string?>(null);
    }

    public ClaimsPrincipal? ValidateJwtToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "your-secret-key-min-32-characters-long-for-security");
        
        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "anticipack-api",
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"] ?? "anticipack-app",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    // Helper method to store refresh tokens (in production, use a database or Redis)
    public void StoreRefreshToken(string refreshToken, string userId, DateTime expiresAt)
    {
        _refreshTokens[refreshToken] = (userId, expiresAt);
    }
}
