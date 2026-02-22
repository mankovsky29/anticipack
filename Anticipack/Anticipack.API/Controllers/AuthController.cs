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
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService authService,
        IUserRepository userRepository,
        ISettingsRepository settingsRepository,
        IConfiguration configuration)
    {
        _authService = authService;
        _userRepository = userRepository;
        _settingsRepository = settingsRepository;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            string? userId = null;
            string? email = null;
            string? name = null;
            string? picture = null;
            AuthProvider provider;

            if (request.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
            {
                var (success, extUserId, extEmail, extName, extPicture) = 
                    await _authService.ValidateGoogleTokenAsync(request.IdToken);
                
                if (!success || extUserId == null || extEmail == null)
                {
                    return BadRequest(new ApiResponse<LoginResponse>(
                        false, null, "Invalid Google token", new List<string> { "Authentication failed" }));
                }

                userId = extUserId;
                email = extEmail;
                name = extName;
                picture = extPicture;
                provider = AuthProvider.Google;
            }
            else if (request.Provider.Equals("Apple", StringComparison.OrdinalIgnoreCase))
            {
                var (success, extUserId, extEmail, extName) = 
                    await _authService.ValidateAppleTokenAsync(request.IdToken);
                
                if (!success || extUserId == null || extEmail == null)
                {
                    return BadRequest(new ApiResponse<LoginResponse>(
                        false, null, "Invalid Apple token", new List<string> { "Authentication failed" }));
                }

                userId = extUserId;
                email = extEmail;
                name = extName;
                provider = AuthProvider.Apple;
            }
            else
            {
                return BadRequest(new ApiResponse<LoginResponse>(
                    false, null, "Invalid provider", new List<string> { "Provider must be Google or Apple" }));
            }

            // Check if user exists
            var user = await _userRepository.GetByExternalAuthIdAsync(userId, provider);
            
            if (user == null)
            {
                // Create new user
                user = new User
                {
                    Email = email,
                    DisplayName = name,
                    ProfilePictureUrl = picture,
                    AuthProvider = provider,
                    ExternalAuthId = userId,
                    LastLoginAt = DateTime.UtcNow
                };
                user = await _userRepository.CreateAsync(user);

                // Create default settings
                var settings = new UserSettings { UserId = user.Id };
                await _settingsRepository.CreateAsync(settings);
            }
            else
            {
                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            // Generate tokens
            var accessToken = _authService.GenerateJwtToken(user.Id, user.Email);
            var refreshToken = _authService.GenerateRefreshToken();
            
            // Store refresh token (in production, use database or Redis)
            if (_authService is AuthService authServiceImpl)
            {
                authServiceImpl.StoreRefreshToken(refreshToken, user.Id, DateTime.UtcNow.AddDays(30));
            }

            var userDto = new UserDto(
                user.Id,
                user.Email,
                user.DisplayName,
                user.ProfilePictureUrl,
                user.AuthProvider.ToString(),
                user.CreatedAt,
                user.LastLoginAt
            );

            var response = new LoginResponse(
                accessToken,
                refreshToken,
                userDto,
                DateTime.UtcNow.AddHours(24)
            );

            return Ok(new ApiResponse<LoginResponse>(true, response, "Login successful"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<LoginResponse>(
                false, null, "Internal server error", new List<string> { ex.Message }));
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var userId = await _authService.ValidateRefreshTokenAsync(request.RefreshToken);

        if (userId == null)
        {
            return Unauthorized(new ApiResponse<LoginResponse>(
                false, null, "Invalid refresh token", new List<string> { "Token validation failed" }));
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new ApiResponse<LoginResponse>(
                false, null, "User not found", new List<string> { "User does not exist" }));
        }

        var accessToken = _authService.GenerateJwtToken(user.Id, user.Email);
        var newRefreshToken = _authService.GenerateRefreshToken();

        if (_authService is AuthService authServiceImpl)
        {
            authServiceImpl.StoreRefreshToken(newRefreshToken, user.Id, DateTime.UtcNow.AddDays(30));
        }

        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.ProfilePictureUrl,
            user.AuthProvider.ToString(),
            user.CreatedAt,
            user.LastLoginAt
        );

        var response = new LoginResponse(
            accessToken,
            newRefreshToken,
            userDto,
            DateTime.UtcNow.AddHours(24)
        );

        return Ok(new ApiResponse<LoginResponse>(true, response, "Token refreshed"));
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
                    LastLoginAt = DateTime.UtcNow
                };
                user = await _userRepository.CreateAsync(user);

                var settings = new UserSettings { UserId = user.Id };
                await _settingsRepository.CreateAsync(settings);
            }
            else
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            var accessToken = _authService.GenerateJwtToken(user.Id, user.Email);
            var refreshToken = _authService.GenerateRefreshToken();

            if (_authService is AuthService authServiceImpl)
            {
                authServiceImpl.StoreRefreshToken(refreshToken, user.Id, DateTime.UtcNow.AddDays(30));
            }

            var userDto = new UserDto(
                user.Id,
                user.Email,
                user.DisplayName,
                user.ProfilePictureUrl,
                user.AuthProvider.ToString(),
                user.CreatedAt,
                user.LastLoginAt
            );

            var response = new LoginResponse(
                accessToken,
                refreshToken,
                userDto,
                DateTime.UtcNow.AddHours(24)
            );

            return Ok(new ApiResponse<LoginResponse>(true, response, "Telegram login successful"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<LoginResponse>(
                false, null, "Internal server error", new List<string> { ex.Message }));
        }
    }
}
