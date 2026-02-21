using Anticipack.Components.Shared.DialogComponent;
using Anticipack.Components.Shared.NavigationHeaderComponent;
using Anticipack.Components.Shared.ToastComponent;
using Anticipack.Packing;
using Anticipack.Resources.Localization;
using Anticipack.Services.Packing;
using Anticipack.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Anticipack.Components.Features.Packing;

public partial class PlayPacking : IDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IPackingRepository PackingRepository { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private INavigationHeaderService NavigationHeaderService { get; set; } = default!;
    [Inject] private IPackingHistoryService PackingHistoryService { get; set; } = default!;

    [Parameter]
    public string Id { get; set; } = string.Empty;

    private const int PackedItemAnimationDelayMs = 350;

    private bool _isLoading;
    private DateTime _packingStartTime;
    private bool _showHistory = false;
    private bool _completionAcknowledged = false;
    private bool _hideCheckedItems = true;
    private bool _wasCompleteOnLoad = false; // Track if all items were already checked when page loaded

    private Storage.PackingActivity _currentActivity = new Storage.PackingActivity();
    
    // History data
    private List<Storage.PackingHistoryEntry> _recentHistory = new();
    private string _averagePackingTime = "-";
    private string _lastPackedDate = "-";
    private PackingTimeComparison? _timeComparison;

    private ElementReference contentArea;
    private int _currentFocusIndex = -1;

    private List<PackingItemView> Items { get; set; } = new();
    private List<string> _categoryOrder = new();
    private readonly Dictionary<string, bool?> _manualOverride = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        _packingStartTime = DateTime.Now;

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
                await PackingRepository.AddOrUpdateAsync(_currentActivity);
                
                // Also update items in repository
                foreach (var item in Items)
                {
                    item.Item.IsPacked = false;
                    await PackingRepository.AddOrUpdateItemAsync(item.Item);
                }
            }

            // Check if all items were already complete when loaded (only relevant if not reset)
            _wasCompleteOnLoad = !shouldResetItems && Items.Any() && Items.All(i => i.IsChecked);
            if (_wasCompleteOnLoad)
            {
                _completionAcknowledged = true; // Don't show modal for already completed packing
            }

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
            
            // Load average packing time
            var avgTime = await PackingHistoryService.GetAveragePackingTimeAsync(activityId);
            _averagePackingTime = avgTime is null 
                ? "-" 
                : $"{(int)avgTime.Value.TotalMinutes} min";
            
            // Load last packed date
            var lastSession = await PackingHistoryService.GetLastPackingSessionAsync(activityId);
            _lastPackedDate = lastSession != null 
                ? lastSession.CompletedDate.ToString("MMM d") 
                : "-";
        }
        catch
        {
            // Silently fail if history loading fails
            _averagePackingTime = "-";
            _lastPackedDate = "-";
        }
    }

    private void ToggleCategory(string category)
    {
        var currentlyCollapsed = IsCollapsed(category);
        _manualOverride[category] = !currentlyCollapsed;
    }

    private void SetItemChecked(PackingItemView item, bool isChecked)
    {
        item.IsChecked = isChecked;
        UpdateCategoryStateAfterItemChange(item.Item.Category);
        SaveCheckState(item);
    }

    private void ToggleItemChecked(PackingItemView item)
    {
        item.IsChecked = !item.IsChecked;
        UpdateCategoryStateAfterItemChange(item.Item.Category);
        SaveCheckState(item);
    }

    private async void SaveCheckState(PackingItemView item)
    {
        if (!string.IsNullOrEmpty(Id))
        {
            try
            {
                item.Item.IsPacked = item.IsChecked;
                await PackingRepository.AddOrUpdateItemAsync(item.Item);
            }
            catch (Exception ex)
            {
                ToastService.ShowError(string.Format(AppResources.ErrorSavingItemState, ex.Message));
            }
        }
    }

    private void UpdateCategoryStateAfterItemChange(string category)
    {
        var allChecked = IsAllChecked(category);

        if (allChecked)
            _manualOverride[category] = true; // Auto-collapse category when all its items are packed

        StateHasChanged();

        if (GetCompletionPercentage() == 100 && !_completionAcknowledged)
        {
            // Trigger completion modal - data calculated in AcknowledgeCompletion
            StateHasChanged();
        }
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

    private void HandleKeyboardNavigation(KeyboardEventArgs e)
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
                    ToggleItemChecked(itemsList[_currentFocusIndex]);
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

    private string GetEstimatedTimeRemaining()
    {
        var itemsLeft = Items.Count(i => !i.IsChecked);
        
        if (itemsLeft == 0)
            return AppResources.Done;
        
        // Use default estimation: 30 seconds per item
        // Cannot use async calls in synchronous rendering methods - causes deadlocks
        var defaultMinutes = Math.Ceiling(itemsLeft * 0.5);
        return string.Format(AppResources.MinRemaining, defaultMinutes);
    }

    private string GetHistoryEntryDescription(Storage.PackingHistoryEntry entry)
    {
        var durationMinutes = (int)(entry.DurationSeconds / 60.0);
        return $"Packed {entry.PackedItems} items in {durationMinutes} minutes";
    }

    private string GetPackingDuration()
    {
        var duration = DateTime.Now - _packingStartTime;
        return $"{(int)duration.TotalMinutes} minutes";
    }

    private async Task AcknowledgeCompletion()
    {
        try
        {
            var endTime = DateTime.Now;
            var packedItems = Items.Count(i => i.IsChecked);
            
            // Compare with average
            _timeComparison = await PackingHistoryService.CompareWithAverageAsync(Id, endTime - _packingStartTime);
            
            // Record completion in history
            await PackingHistoryService.RecordPackingSessionAsync(
                Id, 
                _packingStartTime, 
                endTime, 
                Items.Count, 
                packedItems
            );
            
            // Update activity
            _currentActivity.LastPacked = endTime;
            _currentActivity.RunCount++;
            _currentActivity.IsFinished = true; // Mark as finished
            
            // For one-time activities, automatically archive after completion
            if (!_currentActivity.IsRecurring)
            {
                _currentActivity.IsArchived = true;
            }

            // Save to repository
            if (!string.IsNullOrWhiteSpace(Id))
            {
                await PackingRepository.AddOrUpdateAsync(_currentActivity);
                
                // Reload history data
                await LoadHistoryDataAsync(Id);
            }
            
            _completionAcknowledged = true;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error saving completion: {ex.Message}");
            _completionAcknowledged = true;
        }
    }

    private async Task SwitchToEditMode()
    {
        if (!string.IsNullOrEmpty(Id))
        {
            Navigation.NavigateTo($"/edit-packing/{Id}");
        }
    }

    public void Dispose()
    {
        // No toast cleanup needed anymore
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
        // Count how many items were checked before reset
        int checkedItemsCount = Items.Count(i => i.IsChecked);
        
        foreach (var item in Items)
        {
            item.IsChecked = false;
            item.Item.IsPacked = false;
        }
        
        // Reset completion acknowledgment so modal can show again if user completes it
        _completionAcknowledged = false;
        _wasCompleteOnLoad = false;

        // Reset packing start time for fresh duration tracking
        _packingStartTime = DateTime.Now;
        
        // Save to repository
        if (!string.IsNullOrEmpty(Id))
        {
            try
            {
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
                SaveCheckState(item);
            }
            catch (Exception ex)
            {
                // Fallback if animation fails
                item.IsChecked = isChecked;
                UpdateCategoryStateAfterItemChange(item.Item.Category);
                SaveCheckState(item);
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
            SaveCheckState(item);
            
            if (_hideCheckedItems)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
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
            string.Format(AppResources.ConfirmFinishPackingWithAutoCheck, packedCount, totalCount, remainingCount),
            AppResources.FinishNow,
            AppResources.Cancel);

        if (confirm)
        {
            await FinishPackingNow();
        }
    }

    private async Task FinishPackingNow()
    {
        try
        {
            // Automatically check all remaining unchecked items
            var uncheckedItems = Items.Where(i => !i.IsChecked).ToList();
            foreach (var item in uncheckedItems)
            {
                item.IsChecked = true;
                item.Item.IsPacked = true;
                await PackingRepository.AddOrUpdateItemAsync(item.Item);
            }
            
            var endTime = DateTime.Now;
            var packedItems = Items.Count; // All items are now packed
            
            // Compare with average
            _timeComparison = await PackingHistoryService.CompareWithAverageAsync(Id, endTime - _packingStartTime);
            
            // Record completion in history with all items packed
            await PackingHistoryService.RecordPackingSessionAsync(
                Id, 
                _packingStartTime, 
                endTime, 
                Items.Count, 
                packedItems
            );
            
            // Update activity
            _currentActivity.LastPacked = endTime;
            _currentActivity.RunCount++;
            _currentActivity.IsFinished = true; // Mark as finished
            
            // For one-time activities, automatically archive after completion
            if (!_currentActivity.IsRecurring)
            {
                _currentActivity.IsArchived = true;
            }

            // Save to repository
            if (!string.IsNullOrWhiteSpace(Id))
            {
                await PackingRepository.AddOrUpdateAsync(_currentActivity);
                
                // Reload history data
                await LoadHistoryDataAsync(Id);
            }
            
            // Update UI state for all categories
            foreach (var category in _categoryOrder)
            {
                UpdateCategoryStateAfterItemChange(category);
            }
            
            // Show completion modal
            _completionAcknowledged = false;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error finishing packing: {ex.Message}");
        }
    }
}
