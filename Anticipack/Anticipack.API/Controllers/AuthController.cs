using Anticipack.API.DTOs;
using Anticipack.API.Models;
using Anticipack.API.Repositories;
using Anticipack.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Anticipack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private static readonly AuthProvider[] _federatedProviders = [AuthProvider.Google, AuthProvider.Facebook, AuthProvider.Apple];

    private readonly IAuthService _authService;
    private readonly IIdentityService _identityService;
    private readonly IUserRepository _userRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService authService,
        IIdentityService identityService,
        IUserRepository userRepository,
        ISettingsRepository settingsRepository,
        IConfiguration configuration)
    {
        _authService = authService;
        _identityService = identityService;
        _userRepository = userRepository;
        _settingsRepository = settingsRepository;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        if (!Enum.TryParse<AuthProvider>(request.Provider, true, out var provider))
        {
            return BadRequest(new ApiResponse<LoginResponse>(
                false, null, "Invalid provider", new List<string> { "Provider must be Google, Facebook, or Apple" }));
        }

        return await ExchangeInternalAsync(provider, request.IdToken, request.DeviceId);
    }

    [HttpPost("exchange")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Exchange([FromBody] ExchangeTokenRequest request)
    {
        return await ExchangeInternalAsync(request.Provider, request.IdentityToken, request.DeviceId);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var refreshTokenEntity = await _authService.ValidateRefreshTokenAsync(request.RefreshToken);
        if (refreshTokenEntity == null)
        {
            return Unauthorized(new ApiResponse<LoginResponse>(
                false, null, "Invalid refresh token", new List<string> { "Token validation failed" }));
        }

        var user = await _userRepository.GetByIdAsync(refreshTokenEntity.UserId);
        if (user == null || !user.IsActive)
        {
            return Unauthorized(new ApiResponse<LoginResponse>(
                false, null, "User is not active", new List<string> { "Token validation failed" }));
        }

        var requestIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _authService.RevokeRefreshTokenAsync(request.RefreshToken, requestIp);

        var newRefreshToken = _authService.GenerateRefreshToken();
        await _authService.CreateRefreshTokenAsync(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(30), requestIp);

        var accessToken = _authService.GenerateJwtToken(user.Id, user.Email);
        var response = BuildLoginResponse(user, accessToken, newRefreshToken);

        return Ok(new ApiResponse<LoginResponse>(true, response, "Token refreshed"));
    }

    [HttpPost("revoke")]
    public async Task<ActionResult<ApiResponse<bool>>> Revoke([FromBody] RevokeTokenRequest request)
    {
        var requestIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var revoked = await _authService.RevokeRefreshTokenAsync(request.RefreshToken, requestIp);

        if (!revoked)
        {
            return NotFound(new ApiResponse<bool>(
                false, false, "Refresh token not found", new List<string> { "Token may be invalid, expired, or already revoked" }));
        }

        return Ok(new ApiResponse<bool>(true, true, "Refresh token revoked"));
    }

    [HttpPost("telegram")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> TelegramLogin([FromBody] TelegramLoginRequest request)
    {
        try
        {
            var botApiKey = _configuration["Telegram:BotApiKey"];
            var requestApiKey = Request.Headers["X-Bot-Api-Key"].FirstOrDefault();

            if (string.IsNullOrEmpty(botApiKey) || botApiKey != requestApiKey)
            {
                return Unauthorized(new ApiResponse<LoginResponse>(
                    false, null, "Invalid bot API key", new List<string> { "Authentication failed" }));
            }

            var externalId = request.TelegramUserId.ToString();
            var user = await _userRepository.GetByExternalAuthIdAsync(externalId, AuthProvider.Telegram);

            if (user == null)
            {
                var displayName = string.IsNullOrEmpty(request.LastName)
                    ? request.FirstName ?? "Telegram User"
                    : $"{request.FirstName} {request.LastName}";

                user = new User
                {
                    Email = $"tg_{request.TelegramUserId}@telegram.user",
                    DisplayName = displayName,
                    AuthProvider = AuthProvider.Telegram,
                    ExternalAuthId = externalId,
                    TelegramId = externalId,
                    LastLoginAt = DateTime.UtcNow
                };
                user = await _userRepository.CreateAsync(user);

                var settings = new UserSettings { UserId = user.Id };
                await _settingsRepository.CreateAsync(settings);
            }
            else
            {
                user.LastLoginAt = DateTime.UtcNow;
                user = await _userRepository.UpdateAsync(user);
            }

            var accessToken = _authService.GenerateJwtToken(user.Id, user.Email);
            var refreshToken = _authService.GenerateRefreshToken();
            var requestIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _authService.CreateRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(30), requestIp);

            var response = BuildLoginResponse(user, accessToken, refreshToken);
            return Ok(new ApiResponse<LoginResponse>(true, response, "Telegram login successful"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<LoginResponse>(
                false, null, "Internal server error", new List<string> { ex.Message }));
        }
    }

    private async Task<ActionResult<ApiResponse<LoginResponse>>> ExchangeInternalAsync(AuthProvider provider, string identityToken, string? deviceId)
    {
        try
        {
            if (!_federatedProviders.Contains(provider))
            {
                return BadRequest(new ApiResponse<LoginResponse>(
                    false, null, "Invalid provider", new List<string> { "Provider must be Google, Facebook, or Apple" }));
            }

            var externalIdentity = await _identityService.VerifyIdentityTokenAsync(provider, identityToken);
            if (externalIdentity == null)
            {
                return Unauthorized(new ApiResponse<LoginResponse>(
                    false, null, "Invalid identity token", new List<string> { "Authentication failed" }));
            }

            var normalizedEmail = externalIdentity.Email.Trim().ToLowerInvariant();
            var user = await _userRepository.GetByExternalAuthIdAsync(externalIdentity.ProviderUserId, provider)
                ?? await _userRepository.GetByEmailAsync(normalizedEmail);

            if (user == null)
            {
                user = new User
                {
                    Email = normalizedEmail,
                    DisplayName = externalIdentity.Name,
                    ProfilePictureUrl = externalIdentity.PictureUrl,
                    AuthProvider = provider,
                    ExternalAuthId = externalIdentity.ProviderUserId,
                    LastLoginAt = DateTime.UtcNow
                };
                ApplyProviderId(user, provider, externalIdentity.ProviderUserId);

                user = await _userRepository.CreateAsync(user);
                await _settingsRepository.CreateAsync(new UserSettings { UserId = user.Id });
            }
            else
            {
                user.Email = normalizedEmail;
                user.DisplayName ??= externalIdentity.Name;
                user.ProfilePictureUrl ??= externalIdentity.PictureUrl;
                user.AuthProvider = provider;
                user.ExternalAuthId = externalIdentity.ProviderUserId;
                user.LastLoginAt = DateTime.UtcNow;

                ApplyProviderId(user, provider, externalIdentity.ProviderUserId);
                user = await _userRepository.UpdateAsync(user);
            }

            var accessToken = _authService.GenerateJwtToken(user.Id, user.Email, deviceId);
            var refreshToken = _authService.GenerateRefreshToken();
            var requestIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _authService.CreateRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(30), requestIp);

            var response = BuildLoginResponse(user, accessToken, refreshToken);
            return Ok(new ApiResponse<LoginResponse>(true, response, "Login successful"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<LoginResponse>(
                false, null, "Internal server error", new List<string> { ex.Message }));
        }
    }

    private static void ApplyProviderId(User user, AuthProvider provider, string providerUserId)
    {
        switch (provider)
        {
            case AuthProvider.Google:
                user.GoogleId = providerUserId;
                break;
            case AuthProvider.Facebook:
                user.FacebookId = providerUserId;
                break;
            case AuthProvider.Apple:
                user.AppleId = providerUserId;
                break;
            case AuthProvider.Telegram:
                user.TelegramId = providerUserId;
                break;
        }
    }

    private static LoginResponse BuildLoginResponse(User user, string accessToken, string refreshToken)
    {
        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.ProfilePictureUrl,
            user.AuthProvider.ToString(),
            user.CreatedAt,
            user.LastLoginAt
        );

        return new LoginResponse(
            accessToken,
            refreshToken,
            userDto,
            DateTime.UtcNow.AddMinutes(15)
        );
    }
}
