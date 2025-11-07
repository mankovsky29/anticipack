using Anticipack.API.DTOs;
using Anticipack.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Anticipack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new ApiResponse<UserDto>(
                false, null, "User not found", new List<string> { "User does not exist" }));
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

        return Ok(new ApiResponse<UserDto>(true, userDto));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateCurrentUser([FromBody] UpdateUserRequest request)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new ApiResponse<UserDto>(
                false, null, "User not found", new List<string> { "User does not exist" }));
        }

        if (request.DisplayName != null)
            user.DisplayName = request.DisplayName;
        
        if (request.ProfilePictureUrl != null)
            user.ProfilePictureUrl = request.ProfilePictureUrl;

        user = await _userRepository.UpdateAsync(user);

        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.ProfilePictureUrl,
            user.AuthProvider.ToString(),
            user.CreatedAt,
            user.LastLoginAt
        );

        return Ok(new ApiResponse<UserDto>(true, userDto, "User updated successfully"));
    }

    [HttpDelete("me")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCurrentUser()
    {
        var userId = GetUserId();
        var result = await _userRepository.DeleteAsync(userId);

        if (!result)
        {
            return NotFound(new ApiResponse<bool>(
                false, false, "User not found", new List<string> { "User does not exist" }));
        }

        return Ok(new ApiResponse<bool>(true, true, "User deleted successfully"));
    }
}
