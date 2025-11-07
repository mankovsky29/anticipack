using Anticipack.API.DTOs;
using Anticipack.API.Models;
using Anticipack.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Anticipack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUserRepository _userRepository;

    public SettingsController(
        ISettingsRepository settingsRepository,
        IUserRepository userRepository)
    {
        _settingsRepository = settingsRepository;
        _userRepository = userRepository;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> GetSettings()
    {
        var userId = GetUserId();
        var settings = await _settingsRepository.GetByUserIdAsync(userId);

        if (settings == null)
        {
            // Create default settings if they don't exist
            settings = new UserSettings { UserId = userId };
            settings = await _settingsRepository.CreateAsync(settings);
        }

        var settingsDto = new UserSettingsDto(
            settings.Id,
            settings.EnableNotifications,
            settings.EnableEmailNotifications,
            settings.EnablePushNotifications,
            settings.Theme,
            settings.DefaultCategory,
            settings.AutoResetPackedItems,
            settings.ReminderHoursBeforePacking,
            settings.AllowDataCollection,
            settings.ShareAnonymousUsage,
            settings.Language,
            settings.DateFormat,
            settings.ShowCompletedActivities,
            settings.UpdatedAt
        );

        return Ok(new ApiResponse<UserSettingsDto>(true, settingsDto));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        var userId = GetUserId();
        var settings = await _settingsRepository.GetByUserIdAsync(userId);

        if (settings == null)
        {
            return NotFound(new ApiResponse<UserSettingsDto>(
                false, null, "Settings not found", new List<string> { "User settings do not exist" }));
        }

        // Update only provided fields
        if (request.EnableNotifications.HasValue)
            settings.EnableNotifications = request.EnableNotifications.Value;
        
        if (request.EnableEmailNotifications.HasValue)
            settings.EnableEmailNotifications = request.EnableEmailNotifications.Value;
        
        if (request.EnablePushNotifications.HasValue)
            settings.EnablePushNotifications = request.EnablePushNotifications.Value;
        
        if (request.Theme != null)
            settings.Theme = request.Theme;
        
        if (request.DefaultCategory != null)
            settings.DefaultCategory = request.DefaultCategory;
        
        if (request.AutoResetPackedItems.HasValue)
            settings.AutoResetPackedItems = request.AutoResetPackedItems.Value;
        
        if (request.ReminderHoursBeforePacking.HasValue)
            settings.ReminderHoursBeforePacking = request.ReminderHoursBeforePacking.Value;
        
        if (request.AllowDataCollection.HasValue)
            settings.AllowDataCollection = request.AllowDataCollection.Value;
        
        if (request.ShareAnonymousUsage.HasValue)
            settings.ShareAnonymousUsage = request.ShareAnonymousUsage.Value;
        
        if (request.Language != null)
            settings.Language = request.Language;
        
        if (request.DateFormat != null)
            settings.DateFormat = request.DateFormat;
        
        if (request.ShowCompletedActivities.HasValue)
            settings.ShowCompletedActivities = request.ShowCompletedActivities.Value;

        settings.UpdatedAt = DateTime.UtcNow;
        settings = await _settingsRepository.UpdateAsync(settings);

        var settingsDto = new UserSettingsDto(
            settings.Id,
            settings.EnableNotifications,
            settings.EnableEmailNotifications,
            settings.EnablePushNotifications,
            settings.Theme,
            settings.DefaultCategory,
            settings.AutoResetPackedItems,
            settings.ReminderHoursBeforePacking,
            settings.AllowDataCollection,
            settings.ShareAnonymousUsage,
            settings.Language,
            settings.DateFormat,
            settings.ShowCompletedActivities,
            settings.UpdatedAt
        );

        return Ok(new ApiResponse<UserSettingsDto>(true, settingsDto, "Settings updated successfully"));
    }

    [HttpPost("reset")]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> ResetSettings()
    {
        var userId = GetUserId();
        var settings = await _settingsRepository.GetByUserIdAsync(userId);

        if (settings == null)
        {
            return NotFound(new ApiResponse<UserSettingsDto>(
                false, null, "Settings not found", new List<string> { "User settings do not exist" }));
        }

        // Reset to defaults
        settings.EnableNotifications = true;
        settings.EnableEmailNotifications = false;
        settings.EnablePushNotifications = true;
        settings.Theme = "System";
        settings.DefaultCategory = "General";
        settings.AutoResetPackedItems = true;
        settings.ReminderHoursBeforePacking = 24;
        settings.AllowDataCollection = true;
        settings.ShareAnonymousUsage = true;
        settings.Language = "en";
        settings.DateFormat = "MM/dd/yyyy";
        settings.ShowCompletedActivities = true;
        settings.UpdatedAt = DateTime.UtcNow;

        settings = await _settingsRepository.UpdateAsync(settings);

        var settingsDto = new UserSettingsDto(
            settings.Id,
            settings.EnableNotifications,
            settings.EnableEmailNotifications,
            settings.EnablePushNotifications,
            settings.Theme,
            settings.DefaultCategory,
            settings.AutoResetPackedItems,
            settings.ReminderHoursBeforePacking,
            settings.AllowDataCollection,
            settings.ShareAnonymousUsage,
            settings.Language,
            settings.DateFormat,
            settings.ShowCompletedActivities,
            settings.UpdatedAt
        );

        return Ok(new ApiResponse<UserSettingsDto>(true, settingsDto, "Settings reset to defaults"));
    }
}
