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
    private readonly IPackingHistoryRepository _historyRepository;

    public ActivitiesController(
        IActivityRepository activityRepository,
        IPackingItemRepository itemRepository,
        IPackingHistoryRepository historyRepository)
    {
        _activityRepository = activityRepository;
        _itemRepository = itemRepository;
        _historyRepository = historyRepository;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    private async Task<ActivityDto> ToActivityDto(PackingActivity activity, List<PackingItemDto>? itemDtos = null)
    {
        if (itemDtos == null)
        {
            var items = await _itemRepository.GetByActivityIdAsync(activity.Id);
            itemDtos = items.Select(i => new PackingItemDto(
                i.Id, i.Name, i.IsPacked, i.Category, i.Notes, i.SortOrder)).ToList();
        }

        return new ActivityDto(
            activity.Id,
            activity.UserId,
            activity.Name,
            activity.LastPacked,
            activity.RunCount,
            activity.IsShared,
            activity.IsArchived,
            activity.IsFinished,
            activity.IsRecurring,
            activity.CreatedAt,
            activity.UpdatedAt,
            itemDtos
        );
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ActivityDto>>>> GetAllActivities()
    {
        var userId = GetUserId();
        var activities = await _activityRepository.GetByUserIdAsync(userId);

        var activityDtos = new List<ActivityDto>();
        foreach (var activity in activities)
        {
            activityDtos.Add(await ToActivityDto(activity));
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

        return Ok(new ApiResponse<ActivityDto>(true, await ToActivityDto(activity)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ActivityDto>>> CreateActivity([FromBody] CreateActivityRequest request)
    {
        var userId = GetUserId();
        var activity = new PackingActivity
        {
            UserId = userId,
            Name = request.Name,
            IsShared = request.IsShared,
            IsRecurring = request.IsRecurring
        };

        activity = await _activityRepository.CreateAsync(activity);

        var activityDto = await ToActivityDto(activity, new List<PackingItemDto>());

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
        activity.IsShared = request.IsShared;
        activity.IsRecurring = request.IsRecurring;
        activity.UpdatedAt = DateTime.UtcNow;

        activity = await _activityRepository.UpdateAsync(activity);

        return Ok(new ApiResponse<ActivityDto>(true, await ToActivityDto(activity), "Activity updated successfully"));
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

        return Ok(new ApiResponse<ActivityDto>(true, await ToActivityDto(copiedActivity), "Activity copied successfully"));
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
        activity.IsFinished = false;
        activity.UpdatedAt = DateTime.UtcNow;

        var items = await _itemRepository.GetByActivityIdAsync(id);
        foreach (var item in items)
        {
            item.IsPacked = false;
            await _itemRepository.UpdateAsync(item);
        }

        activity = await _activityRepository.UpdateAsync(activity);

        var itemDtos = items.Select(i => new PackingItemDto(
            i.Id, i.Name, false, i.Category, i.Notes, i.SortOrder)).ToList();

        return Ok(new ApiResponse<ActivityDto>(true, await ToActivityDto(activity, itemDtos), "Packing session started"));
    }

    [HttpPost("{id}/finish")]
    public async Task<ActionResult<ApiResponse<ActivityDto>>> FinishPacking(string id)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(id);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<ActivityDto>(
                false, null, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        activity.IsFinished = true;
        activity.UpdatedAt = DateTime.UtcNow;
        activity = await _activityRepository.UpdateAsync(activity);

        return Ok(new ApiResponse<ActivityDto>(true, await ToActivityDto(activity), "Packing finished"));
    }

    [HttpPost("{id}/archive")]
    public async Task<ActionResult<ApiResponse<ActivityDto>>> ArchiveActivity(string id)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(id);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<ActivityDto>(
                false, null, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        activity.IsArchived = !activity.IsArchived;
        activity.UpdatedAt = DateTime.UtcNow;
        activity = await _activityRepository.UpdateAsync(activity);

        var message = activity.IsArchived ? "Activity archived" : "Activity unarchived";
        return Ok(new ApiResponse<ActivityDto>(true, await ToActivityDto(activity), message));
    }

    // History endpoints
    [HttpGet("{activityId}/history")]
    public async Task<ActionResult<ApiResponse<List<PackingHistoryEntryDto>>>> GetHistory(string activityId, [FromQuery] int count = 10)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(activityId);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<List<PackingHistoryEntryDto>>(
                false, null, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        var entries = await _historyRepository.GetByActivityIdAsync(activityId, count);
        var dtos = entries.Select(e => new PackingHistoryEntryDto(
            e.Id, e.ActivityId, e.CompletedDate, e.TotalItems, e.PackedItems,
            e.DurationSeconds, e.StartTime, e.EndTime)).ToList();

        return Ok(new ApiResponse<List<PackingHistoryEntryDto>>(true, dtos));
    }

    [HttpPost("{activityId}/history")]
    public async Task<ActionResult<ApiResponse<PackingHistoryEntryDto>>> CreateHistoryEntry(
        string activityId, [FromBody] CreateHistoryEntryRequest request)
    {
        var userId = GetUserId();
        var activity = await _activityRepository.GetByIdAsync(activityId);

        if (activity == null || activity.UserId != userId)
        {
            return NotFound(new ApiResponse<PackingHistoryEntryDto>(
                false, null, "Activity not found", new List<string> { "Activity does not exist or access denied" }));
        }

        var entry = new PackingHistoryEntry
        {
            ActivityId = activityId,
            CompletedDate = DateTime.UtcNow,
            TotalItems = request.TotalItems,
            PackedItems = request.PackedItems,
            DurationSeconds = request.DurationSeconds,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        entry = await _historyRepository.CreateAsync(entry);

        var dto = new PackingHistoryEntryDto(
            entry.Id, entry.ActivityId, entry.CompletedDate, entry.TotalItems,
            entry.PackedItems, entry.DurationSeconds, entry.StartTime, entry.EndTime);

        return Ok(new ApiResponse<PackingHistoryEntryDto>(true, dto, "History entry created"));
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

    [HttpPatch("{activityId}/items/{itemId}/toggle")]
    public async Task<ActionResult<ApiResponse<PackingItemDto>>> ToggleItemPacked(string activityId, string itemId)
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

        item.IsPacked = !item.IsPacked;
        item = await _itemRepository.UpdateAsync(item);

        var itemDto = new PackingItemDto(
            item.Id, item.Name, item.IsPacked, item.Category, item.Notes, item.SortOrder);

        return Ok(new ApiResponse<PackingItemDto>(true, itemDto, item.IsPacked ? "Item packed" : "Item unpacked"));
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
