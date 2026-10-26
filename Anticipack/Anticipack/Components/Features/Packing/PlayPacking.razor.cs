using Anticipack.Components.Shared.DialogComponent;
using Anticipack.Components.Shared.NavigationHeaderComponent;
using Anticipack.Components.Shared.ToastComponent;
using Anticipack.Packing;
using Anticipack.Resources.Localization;
using Anticipack.Services;
using Anticipack.Services.Packing;
using Anticipack.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace Anticipack.Components.Features.Packing;

public partial class PlayPacking
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IPackingRepository PackingRepository { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private INavigationHeaderService NavigationHeaderService { get; set; } = default!;
    [Inject] private IPackingHistoryService PackingHistoryService { get; set; } = default!;
    [Inject] private ILocalizationService LocalizationService { get; set; } = default!;
    [Inject] private IStringLocalizer<AppResources> Localizer { get; set; } = default!;

    [Parameter]
    public string Id { get; set; } = string.Empty;

    private const int PackedItemAnimationDelayMs = 350;

    // Longer pauses between checks are breaks, not packing; only this much of a gap counts as active time
    private static readonly TimeSpan MaxCountedGapBetweenChecks = TimeSpan.FromMinutes(5);

    private bool _isLoading;
    private bool _showHistory = false;
    private bool _hideCheckedItems = true;
    private bool _sessionRecorded = false; // The current session is already in history; don't record it again
    private bool _showCompletionModal = false;

    private Storage.PackingActivity _currentActivity = new Storage.PackingActivity();

    // History data
    private List<Storage.PackingHistoryEntry> _recentHistory = new();
    private string _typicalPackingTime = "-";
    private string _lastPackedDate = "-";
    private PackingTimeComparison? _timeComparison;
    private TimeSpan _lastSessionDuration;

    private ElementReference contentArea;
    private int _currentFocusIndex = -1;

    private List<PackingItemView> Items { get; set; } = new();
    private List<string> _categoryOrder = new();
    private readonly Dictionary<string, bool?> _manualOverride = new(StringComparer.OrdinalIgnoreCase);
    private string SessionSavedMessage => AppResources.ResourceManager.GetString("SessionSavedMessage") ?? "Progress saved successfully.";

    // Category headers are only worth showing when items are actually split across categories
    private bool HasMultipleCategories => _categoryOrder.Count > 1;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(Id))
        {
            await LoadItemsForPackingAsync(Id);
        }

        await base.OnInitializedAsync();
    }

    private async Task LoadItemsForPackingAsync(string id)
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            _currentActivity = await PackingRepository.GetByIdAsync(id) ?? new Storage.PackingActivity(){ Name = AppResources.PackingActivity };
            NavigationHeaderService.SetText(_currentActivity.Name ?? "");
            var itemsFromRepo = await PackingRepository.GetItemsForActivityAsync(id);

            // If activity was previously finished, reset all items to unchecked
            bool shouldResetItems = _currentActivity.IsFinished;

            // Map items from repository and restore/reset their packed state
            Items = itemsFromRepo.Select(pi => new PackingItemView(pi)
            {
                IsChecked = shouldResetItems ? false : pi.IsPacked  // Reset if finished, otherwise restore state
            }).ToList();

            // If we reset items, mark activity as not finished and save
            if (shouldResetItems)
            {
                _currentActivity.IsFinished = false;
                ClearSessionTiming();
                await PackingRepository.AddOrUpdateAsync(_currentActivity);

                // Also update items in repository
                foreach (var item in Items)
                {
                    item.Item.IsPacked = false;
                    await PackingRepository.AddOrUpdateItemAsync(item.Item);
                }
            }

            // A list that was already fully packed when opened is not a new session
            _sessionRecorded = !shouldResetItems && Items.Any() && Items.All(i => i.IsChecked);

            _categoryOrder = Items.Select(i => i.Item.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (!_categoryOrder.Any())
            {
                _categoryOrder.Add(PackingCategory.Miscellaneous.ToString());
            }

            _manualOverride.Clear();
            foreach (var cat in _categoryOrder)
            {
                // If all items in category are checked, collapse it
                var allChecked = IsAllChecked(cat);
                _manualOverride[cat] = allChecked ? true : false;
            }

            // Load history data
            await LoadHistoryDataAsync(id);
        }
        catch
        {
            ToastService.ShowError(AppResources.FailedToLoadItems);
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadHistoryDataAsync(string activityId)
    {
        try
        {
            // Load recent history
            _recentHistory = await PackingHistoryService.GetRecentHistoryAsync(activityId, 5);

            var typicalTime = await PackingHistoryService.GetTypicalPackingTimeAsync(activityId);
            _typicalPackingTime = typicalTime is null
                ? "-"
                : PackingDurationFormatter.Format(typicalTime.Value, Localizer);

            // Load last packed date
            var lastSession = await PackingHistoryService.GetLastPackingSessionAsync(activityId);
            _lastPackedDate = lastSession != null
                ? lastSession.CompletedDate.ToString("MMM d")
                : "-";
        }
        catch
        {
            // Silently fail if history loading fails
            _typicalPackingTime = "-";
            _lastPackedDate = "-";
        }
    }

    private void ToggleCategory(string category)
    {
        var currentlyCollapsed = IsCollapsed(category);
        _manualOverride[category] = !currentlyCollapsed;
    }

    private async Task ToggleItemChecked(PackingItemView item)
    {
        item.IsChecked = !item.IsChecked;
        UpdateCategoryStateAfterItemChange(item.Item.Category);
        await SaveCheckStateAsync(item);
        await CompleteSessionIfDoneAsync();
    }

    private async Task SaveCheckStateAsync(PackingItemView item)
    {
        if (!string.IsNullOrEmpty(Id))
        {
            try
            {
                item.Item.IsPacked = item.IsChecked;
                await PackingRepository.AddOrUpdateItemAsync(item.Item);
                await TrackSessionActivityAsync();
            }
            catch (Exception ex)
            {
                ToastService.ShowError(string.Format(AppResources.ErrorSavingItemState, ex.Message));
            }
        }
    }

    /// <summary>
    /// Adds the time since the previous check to the session's active time.
    /// The session spans from the first check to the last one, survives leaving
    /// the screen, and ignores long breaks.
    /// </summary>
    private async Task TrackSessionActivityAsync()
    {
        var now = DateTime.Now;

        if (_currentActivity.SessionLastActivityAt is { } lastActivity)
        {
            var gap = now - lastActivity;
            if (gap < TimeSpan.Zero)
                gap = TimeSpan.Zero;
            if (gap > MaxCountedGapBetweenChecks)
                gap = MaxCountedGapBetweenChecks;

            _currentActivity.SessionActiveSeconds += (int)gap.TotalSeconds;
        }
        else
        {
            _currentActivity.SessionStartedAt = now;
            _currentActivity.SessionActiveSeconds = 0;
        }

        _currentActivity.SessionLastActivityAt = now;
        await PackingRepository.AddOrUpdateAsync(_currentActivity);
    }

    private void ClearSessionTiming()
    {
        _currentActivity.SessionStartedAt = null;
        _currentActivity.SessionLastActivityAt = null;
        _currentActivity.SessionActiveSeconds = 0;
    }

    private void UpdateCategoryStateAfterItemChange(string category)
    {
        var allChecked = IsAllChecked(category);

        if (allChecked)
            _manualOverride[category] = true; // Auto-collapse category when all its items are packed

        StateHasChanged();
    }

    private bool IsAllChecked(string category)
    {
        var group = Items.Where(i => string.Equals(i.Item.Category, category, StringComparison.OrdinalIgnoreCase));
        return group.Any() && group.All(i => i.IsChecked);
    }

    private bool IsCollapsed(string category)
    {
        if (_manualOverride.TryGetValue(category, out var manual) && manual.HasValue)
            return manual.Value;

        return IsAllChecked(category);
    }

    private string GetLocalizedCategoryName(string category)
    {
        var resourceKey = LocalizationService.GetLocalizedCategory(category);
        return AppResources.ResourceManager.GetString(resourceKey, LocalizationService.CurrentCulture) ?? category;
    }

    private async Task HandleKeyboardNavigation(KeyboardEventArgs e)
    {
        var itemsList = Items.ToList();

        switch (e.Key)
        {
            case "ArrowDown":
                _currentFocusIndex = Math.Min(_currentFocusIndex + 1, itemsList.Count - 1);
                break;
            case "ArrowUp":
                _currentFocusIndex = Math.Max(_currentFocusIndex - 1, 0);
                break;
            case " ": // Space key
                if (_currentFocusIndex >= 0 && _currentFocusIndex < itemsList.Count)
                    await ToggleItemChecked(itemsList[_currentFocusIndex]);
                break;
        }

        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("focusElement", contentArea);
        }
    }

    private void ToggleHistory()
    {
        _showHistory = !_showHistory;
    }

    private int GetCompletionPercentage() =>
        Items.Any() ? (int)(Items.Count(i => i.IsChecked) * 100.0 / Items.Count) : 0;

    private string GetHistoryEntryDescription(Storage.PackingHistoryEntry entry)
    {
        // Older sessions only have wall-clock duration
        var durationMinutes = (int)((entry.ActiveDurationSeconds ?? entry.DurationSeconds) / 60.0);
        return string.Format(AppResources.PackedItemsInMinutes, entry.PackedItems, durationMinutes);
    }

    private string GetPackingDuration() => PackingDurationFormatter.Format(_lastSessionDuration, Localizer);

    /// <summary>
    /// Records the session as soon as every item is packed, so progress is saved
    /// even if the user leaves without acknowledging the completion dialog.
    /// </summary>
    private async Task CompleteSessionIfDoneAsync()
    {
        if (Items.Any() && Items.All(i => i.IsChecked))
            await CompleteSessionAsync();
    }

    /// <summary>
    /// Records the session; any items still unchecked are stored as skipped
    /// so the user can later review items they rarely take.
    /// </summary>
    private async Task CompleteSessionAsync()
    {
        if (_sessionRecorded || !Items.Any())
            return;

        _sessionRecorded = true;

        try
        {
            var endTime = DateTime.Now;
            var skippedItemIds = Items.Where(i => !i.IsChecked).Select(i => i.Item.Id).ToList();
            var startTime = _currentActivity.SessionStartedAt ?? endTime;
            _lastSessionDuration = TimeSpan.FromSeconds(_currentActivity.SessionActiveSeconds);

            // Compare before recording so the current session doesn't skew the typical time
            _timeComparison = await PackingHistoryService.CompareWithTypicalAsync(Id, _lastSessionDuration);

            await PackingHistoryService.RecordPackingSessionAsync(
                Id,
                startTime,
                endTime,
                _lastSessionDuration,
                Items.Count,
                Items.Count - skippedItemIds.Count,
                skippedItemIds
            );

            // Update activity
            _currentActivity.LastPacked = endTime;
            _currentActivity.RunCount++;
            _currentActivity.IsFinished = true; // Mark as finished
            ClearSessionTiming();

            // For one-time activities, automatically archive after completion
            if (!_currentActivity.IsRecurring)
            {
                _currentActivity.IsArchived = true;
            }

            if (!string.IsNullOrWhiteSpace(Id))
            {
                await PackingRepository.AddOrUpdateAsync(_currentActivity);
                await LoadHistoryDataAsync(Id);
            }

            _showCompletionModal = true;
        }
        catch (Exception ex)
        {
            ToastService.ShowError(string.Format(AppResources.ErrorSavingCompletion, ex.Message));
        }

        await InvokeAsync(StateHasChanged);
    }

    private void AcknowledgeCompletion()
    {
        // The session is saved; packing is done, so return to the activity list
        _showCompletionModal = false;
        Navigation.NavigateTo("/packing-activities");
    }

    private async Task SwitchToEditMode()
    {
        if (!string.IsNullOrEmpty(Id))
        {
            Navigation.NavigateTo($"/edit-packing/{Id}");
        }
    }

    private void ToggleItemVisibility()
    {
        _hideCheckedItems = !_hideCheckedItems;
    }

    private async Task ConfirmResetAllItems()
    {
        bool confirm = await DialogService.ShowConfirmAsync(
            AppResources.ResetPackingList,
            AppResources.ConfirmResetAllItems,
            AppResources.ResetAll,
            AppResources.Cancel);

        if (confirm)
        {
            await ResetAllItems();
            ExpandAllCategories();
        }
    }

    private void ExpandAllCategories()
    {
        foreach (var key in _manualOverride.Keys)
        {
            _manualOverride[key] = false; // false it is expanded
        }
    }

    private async Task ResetAllItems()
    {
        foreach (var item in Items)
        {
            item.IsChecked = false;
            item.Item.IsPacked = false;
        }

        // Start a fresh session that can be recorded again once completed
        _sessionRecorded = false;
        ClearSessionTiming();

        // Save to repository
        if (!string.IsNullOrEmpty(Id))
        {
            try
            {
                await PackingRepository.AddOrUpdateAsync(_currentActivity);

                foreach (var item in Items)
                {
                    await PackingRepository.AddOrUpdateItemAsync(item.Item);
                }

                // Refresh categories state
                foreach (var category in _categoryOrder)
                {
                    UpdateCategoryStateAfterItemChange(category);
                }

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                ToastService.ShowError(string.Format(AppResources.ErrorResettingItems, ex.Message));
            }
        }
    }

    private async Task HandleRowClick(MouseEventArgs e, PackingItemView item)
    {
        await HandleItemChecked(item, !item.IsChecked, e);
    }

    private async Task HandleItemChecked(PackingItemView item, bool isChecked, MouseEventArgs? e)
    {
        // Only apply animation when marking as checked (not unchecking)
        if (!item.IsChecked && isChecked)
        {
            try
            {
                item.IsAnimating = true;
                StateHasChanged(); // Force UI update to show animation starting

                item.IsChecked = isChecked;
                await Task.Delay(PackedItemAnimationDelayMs);

                UpdateCategoryStateAfterItemChange(item.Item.Category);

                // Save the change to repository
                await SaveCheckStateAsync(item);
            }
            catch (Exception ex)
            {
                // Fallback if animation fails
                item.IsChecked = isChecked;
                UpdateCategoryStateAfterItemChange(item.Item.Category);
                await SaveCheckStateAsync(item);
            }
            finally
            {
                // End animation state
                item.IsAnimating = false;

                // Force UI update if needed
                if (_hideCheckedItems)
                {
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        else
        {
            item.IsChecked = isChecked;

            UpdateCategoryStateAfterItemChange(item.Item.Category);
            await SaveCheckStateAsync(item);

            if (_hideCheckedItems)
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        await CompleteSessionIfDoneAsync();
    }

    private bool CanFinishPacking()
    {
        var checkedCount = Items.Count(i => i.IsChecked);
        var totalCount = Items.Count;

        // Can finish if at least one item is checked AND not all items are checked
        return checkedCount > 0 && checkedCount < totalCount;
    }

    private async Task ConfirmFinishPacking()
    {
        var packedCount = Items.Count(i => i.IsChecked);
        var totalCount = Items.Count;
        var remainingCount = totalCount - packedCount;

        bool confirm = await DialogService.ShowConfirmAsync(
            AppResources.FinishPacking,
            string.Format(Localizer["ConfirmFinishPackingWithSkipped"], packedCount, totalCount, remainingCount),
            AppResources.FinishNow,
            AppResources.Cancel);

        if (confirm)
        {
            // Unchecked items stay unpacked and are recorded as skipped, not silently marked as packed
            await CompleteSessionAsync();
        }
    }
}
