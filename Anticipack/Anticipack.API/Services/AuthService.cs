using Anticipack.API.Models;
using Anticipack.API.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Anticipack.API.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthService(IConfiguration configuration, IRefreshTokenRepository refreshTokenRepository)
    {
        _configuration = configuration;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public string GenerateJwtToken(string userId, string email, string? deviceId = null)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "your-secret-key-min-32-characters-long-for-security"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            claims.Add(new Claim("device_id", deviceId));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "anticipack-api",
            audience: _configuration["Jwt:Audience"] ?? "anticipack-app",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
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

    public string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash);
    }

    public Task<RefreshToken> CreateRefreshTokenAsync(string userId, string refreshToken, DateTime expiresAtUtc, string? createdByIp)
    {
        var token = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashRefreshToken(refreshToken),
            ExpiryDate = expiresAtUtc,
            CreatedByIp = createdByIp
        };

        return _refreshTokenRepository.CreateAsync(token);
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);
        if (token == null || !token.IsActive)
        {
            return null;
        }

        return token;
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, string? revokedByIp)
    {
        var token = await ValidateRefreshTokenAsync(refreshToken);
        if (token == null)
        {
            return false;
        }

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = revokedByIp;
        await _refreshTokenRepository.UpdateAsync(token);
        return true;
    }

    public async Task<(RefreshToken RevokedToken, RefreshToken NewToken)?> RotateRefreshTokenAsync(
        string refreshToken,
        string userId,
        DateTime newExpiryDateUtc,
        string? requestIp)
    {
        var current = await ValidateRefreshTokenAsync(refreshToken);
        if (current == null || current.UserId != userId)
        {
            return null;
        }

        var newRefreshToken = GenerateRefreshToken();
        var newToken = await CreateRefreshTokenAsync(userId, newRefreshToken, newExpiryDateUtc, requestIp);

        current.IsRevoked = true;
        current.RevokedAt = DateTime.UtcNow;
        current.RevokedByIp = requestIp;
        current.ReplacedByTokenHash = newToken.TokenHash;
        await _refreshTokenRepository.UpdateAsync(current);

        return (current, newToken);
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
}
