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
public class ActivitiesController : ControllerBase
{
    private readonly IActivityRepository _activityRepository;
    private readonly IPackingItemRepository _itemRepository;

    public ActivitiesController(
        IActivityRepository activityRepository,
        IPackingItemRepository itemRepository)
    {
        _activityRepository = activityRepository;
        _itemRepository = itemRepository;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ActivityDto>>>> GetAllActivities()
    {
        var userId = GetUserId();
        var activities = await _activityRepository.GetByUserIdAsync(userId);

        var activityDtos = new List<ActivityDto>();
        foreach (var activity in activities)
        {
            var items = await _itemRepository.GetByActivityIdAsync(activity.Id);
            var itemDtos = items.Select(i => new PackingItemDto(
                i.Id, i.Name, i.IsPacked, i.Category, i.Notes, i.SortOrder)).ToList();

            activityDtos.Add(new ActivityDto(
                activity.Id,
                activity.UserId,
                activity.Name,
                activity.Category,
                activity.LastPacked,
                activity.RunCount,
                activity.IsShared,
                activity.CreatedAt,
                activity.UpdatedAt,
                itemDtos
            ));
        }

        return Ok(new ApiResponse<List<ActivityDto>>(true, activityDtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ActivityDto>>> GetActivity(string id)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(id);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<ActivityDto>(
                false, null, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        var items = await _itemRepository.GetByActivityIdAsync(activity.Id);
        var itemDtos = items.Select(i => new PackingItemDto(
            i.Id, i.Name, i.IsPacked, i.Category, i.Notes, i.SortOrder)).ToList();

        var activityDto = new ActivityDto(
            activity.Id,
            activity.UserId,
            activity.Name,
            activity.Category,
            activity.LastPacked,
            activity.RunCount,
            activity.IsShared,
            activity.CreatedAt,
            activity.UpdatedAt,
            itemDtos
        );

        return Ok(new ApiResponse<ActivityDto>(true, activityDto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ActivityDto>>> CreateActivity([FromBody] CreateActivityRequest request)
    {
        var userId = GetUserId();
        var activity = new PackingActivity
        {
            UserId = userId,
            Name = request.Name,
            Category = request.Category,
            IsShared = request.IsShared
        };

        activity = await _activityRepository.CreateAsync(activity);

        var activityDto = new ActivityDto(
            activity.Id,
            activity.UserId,
            activity.Name,
            activity.Category,
            activity.LastPacked,
            activity.RunCount,
            activity.IsShared,
            activity.CreatedAt,
            activity.UpdatedAt,
            new List<PackingItemDto>()
        );

        return CreatedAtAction(
            nameof(GetActivity),
            new { id = activity.Id },
            new ApiResponse<ActivityDto>(true, activityDto, "Activity created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ActivityDto>>> UpdateActivity(string id, [FromBody] UpdateActivityRequest request)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(id);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<ActivityDto>(
                false, null, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        activity.Name = request.Name;
        activity.Category = request.Category;
        activity.IsShared = request.IsShared;
        activity.UpdatedAt = DateTime.UtcNow;

        activity = await _activityRepository.UpdateAsync(activity);

        var items = await _itemRepository.GetByActivityIdAsync(activity.Id);
        var itemDtos = items.Select(i => new PackingItemDto(
            i.Id, i.Name, i.IsPacked, i.Category, i.Notes, i.SortOrder)).ToList();

        var activityDto = new ActivityDto(
            activity.Id,
            activity.UserId,
            activity.Name,
            activity.Category,
            activity.LastPacked,
            activity.RunCount,
            activity.IsShared,
            activity.CreatedAt,
            activity.UpdatedAt,
            itemDtos
        );

        return Ok(new ApiResponse<ActivityDto>(true, activityDto, "Activity updated successfully"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteActivity(string id)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(id);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<bool>(
                false, false, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        // Delete all items first
        var items = await _itemRepository.GetByActivityIdAsync(id);
        foreach (var item in items)
        {
            await _itemRepository.DeleteAsync(item.Id);
        }

        var result = await _activityRepository.DeleteAsync(id);
        return Ok(new ApiResponse<bool>(true, result, "Activity deleted successfully"));
    }

    [HttpPost("{id}/copy")]
    public async Task<ActionResult<ApiResponse<ActivityDto>>> CopyActivity(string id)
    {
        var userId = GetUserId();
        var copiedActivity = await _activityRepository.CopyActivityAsync(id, userId);

        if (copiedActivity == null)
        {
            return NotFound(new ApiResponse<ActivityDto>(
                false, null, "Activity not found", new List<string> { "Activity does not exist" }));
        }

        var items = await _itemRepository.GetByActivityIdAsync(copiedActivity.Id);
        var itemDtos = items.Select(i => new PackingItemDto(
            i.Id, i.Name, i.IsPacked, i.Category, i.Notes, i.SortOrder)).ToList();

        var activityDto = new ActivityDto(
            copiedActivity.Id,
            copiedActivity.UserId,
            copiedActivity.Name,
            copiedActivity.Category,
            copiedActivity.LastPacked,
            copiedActivity.RunCount,
            copiedActivity.IsShared,
            copiedActivity.CreatedAt,
            copiedActivity.UpdatedAt,
            itemDtos
        );

        return Ok(new ApiResponse<ActivityDto>(true, activityDto, "Activity copied successfully"));
    }

    [HttpPost("{id}/start")]
    public async Task<ActionResult<ApiResponse<ActivityDto>>> StartPacking(string id)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(id);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<ActivityDto>(
                false, null, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        activity.RunCount++;
        activity.LastPacked = DateTime.UtcNow;
        activity.UpdatedAt = DateTime.UtcNow;

        // Reset all items to unpacked
        var items = await _itemRepository.GetByActivityIdAsync(id);
        foreach (var item in items)
        {
            item.IsPacked = false;
            await _itemRepository.UpdateAsync(item);
        }

        activity = await _activityRepository.UpdateAsync(activity);

        var itemDtos = items.Select(i => new PackingItemDto(
            i.Id, i.Name, false, i.Category, i.Notes, i.SortOrder)).ToList();

        var activityDto = new ActivityDto(
            activity.Id,
            activity.UserId,
            activity.Name,
            activity.Category,
            activity.LastPacked,
            activity.RunCount,
            activity.IsShared,
            activity.CreatedAt,
            activity.UpdatedAt,
            itemDtos
        );

        return Ok(new ApiResponse<ActivityDto>(true, activityDto, "Packing session started"));
    }

    // Item endpoints
    [HttpGet("{activityId}/items")]
    public async Task<ActionResult<ApiResponse<List<PackingItemDto>>>> GetItems(string activityId)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(activityId);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<List<PackingItemDto>>(
                false, null, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        var items = await _itemRepository.GetByActivityIdAsync(activityId);
        var itemDtos = items.Select(i => new PackingItemDto(
            i.Id, i.Name, i.IsPacked, i.Category, i.Notes, i.SortOrder)).ToList();

        return Ok(new ApiResponse<List<PackingItemDto>>(true, itemDtos));
    }

    [HttpPost("{activityId}/items")]
    public async Task<ActionResult<ApiResponse<PackingItemDto>>> CreateItem(string activityId, [FromBody] CreatePackingItemRequest request)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(activityId);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<PackingItemDto>(
                false, null, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        var item = new PackingItem
        {
            ActivityId = activityId,
            Name = request.Name,
            Category = request.Category,
            Notes = request.Notes,
            SortOrder = request.SortOrder
        };

        item = await _itemRepository.CreateAsync(item);

        var itemDto = new PackingItemDto(
            item.Id, item.Name, item.IsPacked, item.Category, item.Notes, item.SortOrder);

        return CreatedAtAction(
            nameof(GetItems),
            new { activityId },
            new ApiResponse<PackingItemDto>(true, itemDto, "Item created successfully"));
    }

    [HttpPut("{activityId}/items/{itemId}")]
    public async Task<ActionResult<ApiResponse<PackingItemDto>>> UpdateItem(
        string activityId, string itemId, [FromBody] UpdatePackingItemRequest request)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(activityId);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<PackingItemDto>(
                false, null, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        var item = await _itemRepository.GetByIdAsync(itemId);
        if (item == null || item.ActivityId != activityId)
        {
            return NotFound(new ApiResponse<PackingItemDto>(
                false, null, "Item not found", new List<string> { "Item does not exist" }));
        }

        item.Name = request.Name;
        item.IsPacked = request.IsPacked;
        item.Category = request.Category;
        item.Notes = request.Notes;
        item.SortOrder = request.SortOrder;

        item = await _itemRepository.UpdateAsync(item);

        var itemDto = new PackingItemDto(
            item.Id, item.Name, item.IsPacked, item.Category, item.Notes, item.SortOrder);

        return Ok(new ApiResponse<PackingItemDto>(true, itemDto, "Item updated successfully"));
    }

    [HttpDelete("{activityId}/items/{itemId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteItem(string activityId, string itemId)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(activityId);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<bool>(
                false, false, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        var item = await _itemRepository.GetByIdAsync(itemId);
        if (item == null || item.ActivityId != activityId)
        {
            return NotFound(new ApiResponse<bool>(
                false, false, "Item not found", new List<string> { "Item does not exist" }));
        }

        var result = await _itemRepository.DeleteAsync(itemId);
        return Ok(new ApiResponse<bool>(true, result, "Item deleted successfully"));
    }
}
