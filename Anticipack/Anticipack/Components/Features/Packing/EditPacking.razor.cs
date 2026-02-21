using System.Text.Json;
using Anticipack.Components.Shared.DialogComponent;
using Anticipack.Components.Shared.NavigationHeaderComponent;
using Anticipack.Components.Shared.ToastComponent;
using Anticipack.Packing;
using Anticipack.Resources.Localization;
using Anticipack.Services;
using Anticipack.Services.Categories;
using Anticipack.Storage;
using Anticipack.Storage.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using DragEventArgs = Microsoft.AspNetCore.Components.Web.DragEventArgs;
using FocusEventArgs = Microsoft.AspNetCore.Components.Web.FocusEventArgs;

namespace Anticipack.Components.Features.Packing;

public partial class EditPacking : IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IPackingRepository PackingRepository { get; set; } = default!;
    [Inject] private IPackingActivityRepository PackingActivityRepository { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private INavigationHeaderService NavigationHeaderService { get; set; } = default!;
    [Inject] private IKeyboardService KeyboardService { get; set; } = default!;
    [Inject] private ILocalizationService LocalizationService { get; set; } = default!;
    [Inject] private MicrophonePermissionBridge PermissionBridge { get; set; } = default!;
    [Inject] private IStringLocalizer<AppResources> Localizer { get; set; } = default!;
    [Inject] private ICategoryIconProvider CategoryIconProvider { get; set; } = default!;

    [Parameter]
    public string Id { get; set; } = string.Empty;
    private bool _isCategoryDropdownOpen = false;
    private bool _isAddingItem = false;
    private bool _isLoading;
    private bool _keyboardVisible;
    private double _keyboardHeight;
    private Storage.PackingActivity _currentActivity = new Storage.PackingActivity();
    private string _activityName = string.Empty;
    private bool _isSettingsExpanded = false; // Collapsed by default
    private bool _isOverflowMenuOpen = false;
    private bool _overflowMenuJustOpened = false;

    // Add item form state
    private bool _isQuickAddMode = true;
    private string _quickAddText = string.Empty;
    private List<string> _parsedItems = [];
    private List<string> _duplicateItems = [];
    private HashSet<string> _existingItemNames = new(StringComparer.OrdinalIgnoreCase);
    private ElementReference quickAddInputRef;
    private bool _isCategoryLocked = false; // When adding from category header, hide category selector
    private string? _addingToCategory = null; // Which category to show the form above (null = top of list)

    private bool HasInput => _isQuickAddMode 
        ? !string.IsNullOrWhiteSpace(_quickAddText) 
        : !string.IsNullOrWhiteSpace(_bulkItemsText);

    private bool CanAdd => _parsedItems.Count > 0;

    private sealed class PackingItemView
    {
        public PackingItemView(Storage.PackingItem item) => Item = item;
        public Storage.PackingItem Item { get; }
        public bool IsChecked { get; set; }
    }

    private PackingItemView? _editingItem = null;
    private ElementReference itemEditInputElement;
    private ElementReference bulkItemsTextarea;

    private string? _editingOriginalName;
    private string? _editingOriginalNotes;

    private List<PackingItemView> Items { get; set; } = new();
    private List<string> _categoryOrder = new();
    private readonly Dictionary<string, bool?> _manualOverride = new(StringComparer.OrdinalIgnoreCase);

    private string _newCategory = string.Empty;
    private string _bulkItemsText = string.Empty;

    private PackingItemView? _draggedItem;
    private PackingItemView? _dragOverItem;
    private bool _isDragging = false;
    private string _dropLinePosition = ""; // "before" or "after"

    private bool _showCategorySelector = false;

    private bool _pendingScrollToAddForm = false;

    // Speech recognition state
    private bool _isSpeechRecognitionSupported = false;
    private bool _isListening = false;
    private DotNetObjectReference<EditPacking>? _dotNetHelper;
    private IJSObjectReference? _editPackingModule;
    private string _interimTranscript = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            KeyboardService.Initialize(this);

            _editPackingModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./Components/Features/Packing/EditPacking.razor.js");

            // Check if speech recognition is supported
            try
            {
                _isSpeechRecognitionSupported = await JSRuntime.InvokeAsync<bool>("isSpeechRecognitionSupported");
            }
            catch
            {
                _isSpeechRecognitionSupported = false;
            }
        }

        if (_pendingScrollToAddForm && _editPackingModule is not null)
        {
            _pendingScrollToAddForm = false;
            try
            {
                await _editPackingModule.InvokeVoidAsync("scrollToAddForm");
            }
            catch { }
        }

        if (_overflowMenuJustOpened && _editPackingModule is not null)
        {
            _overflowMenuJustOpened = false;
            try
            {
                await _editPackingModule.InvokeVoidAsync("positionOverflowMenu");
            }
            catch { }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(Id))
        {
            await LoadItemsForPackingAsync(Id);
        }
        else
        {
            InitializeDefaultItems();
        }

        KeyboardService.KeyboardVisibilityChanged += OnKeyboardVisibilityChanged;

        await base.OnInitializedAsync();
    }

    private void ToggleCategoryDropdown()
    {
        _isCategoryDropdownOpen = !_isCategoryDropdownOpen;
    }

    private void SelectCategoryFromDropdown(PackingCategory category)
    {
        _newCategory = category.ToString();
        _isCategoryDropdownOpen = false;
    }

    private async Task CancelAdd()
    {
        _isAddingItem = false;
        _quickAddText = string.Empty;
        _bulkItemsText = string.Empty;
        _parsedItems = [];
        _duplicateItems = [];
        _isCategoryDropdownOpen = false;
        _isCategoryLocked = false;
        _addingToCategory = null;
        await SyncClickOutsideHandlerAsync();
    }

    private void InitializeDefaultItems()
    {
        _currentActivity = new Storage.PackingActivity { Name = AppResources.NewPackingActivity };
        _activityName = _currentActivity.Name;
        Items = new List<PackingItemView>();
        _categoryOrder = Items.Select(i => i.Item.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var cat in _categoryOrder)
            _manualOverride[cat] = null;
    }

    private async Task LoadItemsForPackingAsync(string id)
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            _currentActivity = await PackingRepository.GetByIdAsync(id) ?? new Storage.PackingActivity(){ Name = AppResources.NewPackingActivity };
            _activityName = _currentActivity.Name ?? AppResources.NewPackingActivity;
            NavigationHeaderService.SetText(_currentActivity.Name ?? "");
            var itemsFromRepo = await PackingRepository.GetItemsForActivityAsync(id);
            Items = itemsFromRepo.Select(pi => new PackingItemView(pi)).ToList();
            _categoryOrder = Items.Select(i => i.Item.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (!_categoryOrder.Any())
            {
                _categoryOrder.Add(PackingCategory.Miscellaneous.ToString());
            }

            _manualOverride.Clear();
            foreach (var cat in _categoryOrder)
                _manualOverride[cat] = null;
        }
        catch (Exception ex)
        {
            ToastService.ShowError(AppResources.FailedToLoadItems);
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void ToggleCategory(string category)
    {
        var currentlyCollapsed = IsCollapsed(category);
        _manualOverride[category] = !currentlyCollapsed;
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

    private async Task ShowAddItemForm()
    {
        await CloseOverflowMenuAsync();
        _isAddingItem = true;
        _isQuickAddMode = true;
        _quickAddText = string.Empty;
        _bulkItemsText = string.Empty;
        _parsedItems = [];
        _duplicateItems = [];
        _isCategoryLocked = false;
        _addingToCategory = null;

        // Cache existing item names for duplicate detection
        _existingItemNames = Items
            .Select(i => i.Item.Name.ToLower())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (_categoryOrder.Any())
        {
            _newCategory = _categoryOrder.First();
        }
        else
        {
            _newCategory = PackingCategory.Miscellaneous.ToString();
        }

        _pendingScrollToAddForm = true;
        await SyncClickOutsideHandlerAsync();
    }

    private async Task ShowAddItemFormForCategory(string category)
    {
        _isAddingItem = true;
        _isQuickAddMode = true;
        _quickAddText = string.Empty;
        _bulkItemsText = string.Empty;
        _parsedItems = [];
        _duplicateItems = [];
        _isCategoryLocked = true; // Hide category selector
        _addingToCategory = category;
        _newCategory = category;

        // Ensure the target category is expanded so the user sees context
        _manualOverride[category] = false;

        // Cache existing item names for duplicate detection
        _existingItemNames = Items
            .Select(i => i.Item.Name.ToLower())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _pendingScrollToAddForm = true;
        await SyncClickOutsideHandlerAsync();
    }

    private void SelectNewCategory(PackingCategory category)
    {
        _newCategory = category.ToString();
    }

    private void SetAddMode(bool isQuickMode)
    {
        if (_isQuickAddMode == isQuickMode) return;

        _isQuickAddMode = isQuickMode;

        // Transfer content between modes
        if (isQuickMode && !string.IsNullOrWhiteSpace(_bulkItemsText))
        {
            // Take first item from bulk text
            var firstItem = ParseInputItems(_bulkItemsText).FirstOrDefault();
            _quickAddText = firstItem ?? string.Empty;
        }
        else if (!isQuickMode && !string.IsNullOrWhiteSpace(_quickAddText))
        {
            _bulkItemsText = _quickAddText;
        }

        UpdateParsedItems();
    }

    private void OnInputChanged()
    {
        UpdateParsedItems();
    }

    private async Task OnBulkTextChanged()
    {
        UpdateParsedItems();
        await HandleTextareaAutoGrow();
    }

    private void UpdateParsedItems()
    {
        var inputText = _isQuickAddMode ? _quickAddText : _bulkItemsText;
        var allItems = ParseInputItems(inputText);

        // Remove duplicates within the input (case-insensitive)
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueItems = new List<string>();
        _duplicateItems = [];

        foreach (var item in allItems)
        {
            if (seen.Add(item))
            {
                uniqueItems.Add(item);
            }
            else
            {
                _duplicateItems.Add(item);
            }
        }

        _parsedItems = uniqueItems;
    }

    private List<string> ParseInputItems(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return text
            .Split(["\r\n", "\r", "\n", ","], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private string GetAddButtonText()
    {
        if (_parsedItems.Count == 0)
            return Localizer["Add"];

        if (_parsedItems.Count == 1)
            return Localizer["AddItem"];

        return string.Format(Localizer["AddItemsCount"], _parsedItems.Count);
    }

    private async Task ConfirmAddAsync()
    {
        if (!CanAdd || string.IsNullOrWhiteSpace(Id))
            return;

        var category = string.IsNullOrWhiteSpace(_newCategory) 
            ? PackingCategory.Miscellaneous.ToString() 
            : _newCategory.Trim();

        int addedCount = 0;
        int skippedCount = 0;

        try
        {
            foreach (var itemName in _parsedItems)
            {
                // Skip items that already exist
                if (_existingItemNames.Contains(itemName.ToLower()))
                {
                    skippedCount++;
                    continue;
                }

                try
                {
                    var newItem = new Storage.PackingItem 
                    { 
                        Name = itemName, 
                        Category = category, 
                        Notes = string.Empty, 
                        ActivityId = Id 
                    };

                    await PackingRepository.AddItemToActivityAsync(Id, newItem);
                    addedCount++;
                }
                catch
                {
                    // Item failed to add, continue with others
                }
            }

            await LoadItemsForPackingAsync(Id);
            _manualOverride[category] = false;


            if (skippedCount > 0)
            {
                ToastService.ShowInfo(string.Format(Localizer["DuplicatesSkipped"], skippedCount));
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError(string.Format(AppResources.ErrorAddingItem, ex.Message));
        }

        // Close form and reset state
        await CancelAdd();
        StateHasChanged();
    }

    private async Task HandleQuickAddKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await ConfirmAddAsync();
        }
        else if (e.Key == "Escape")
        {
            await CancelAdd();
        }
    }

    private async Task ToggleSpeechRecognitionQuickAdd()
    {
        // For quick add, we use the same speech recognition but target the quick add input
        await ToggleSpeechRecognition();
    }

    private void ToggleSettings()
    {
        _isSettingsExpanded = !_isSettingsExpanded;
    }

    private async Task ToggleOverflowMenuAsync()
    {
        _isOverflowMenuOpen = !_isOverflowMenuOpen;
        if (_isOverflowMenuOpen)
        {
            _overflowMenuJustOpened = true;
            _dotNetHelper ??= DotNetObjectReference.Create(this);
            if (_editPackingModule is not null)
                try { await _editPackingModule.InvokeVoidAsync("registerOverflowClickOutside", _dotNetHelper); } catch { }
        }
        else
        {
            if (_editPackingModule is not null)
                try { await _editPackingModule.InvokeVoidAsync("unregisterOverflowClickOutside"); } catch { }
        }
    }

    [JSInvokable]
    public async Task CloseOverflowMenuFromJsAsync()
    {
        _isOverflowMenuOpen = false;
        if (_editPackingModule is not null)
            try { await _editPackingModule.InvokeVoidAsync("unregisterOverflowClickOutside"); } catch { }
        StateHasChanged();
    }

    private async Task CloseOverflowMenuAsync()
    {
        _isOverflowMenuOpen = false;
        if (_editPackingModule is not null)
            try { await _editPackingModule.InvokeVoidAsync("unregisterOverflowClickOutside"); } catch { }
    }

    private async Task SetActivityType(bool isRecurring)
    {
        if (_currentActivity.IsRecurring == isRecurring)
            return;

        _currentActivity.IsRecurring = isRecurring;
        await SaveActivityAsync();
        
        var message = isRecurring 
            ? $"✓ {Localizer["RecurringActivity"]}" 
            : $"✓ {Localizer["OneTimeActivity"]}";
    }

    private async Task SaveArchiveStatusAsync()
    {
        await SaveActivityAsync();
        
        var archiveText = _currentActivity.IsArchived 
            ? Localizer["Archived"] 
            : Localizer["Active"];
        ToastService.ShowSuccess($"✓ {archiveText}");
    }

    private async Task SaveActivityAsync()
    {
        if (string.IsNullOrWhiteSpace(Id))
            return;

        try
        {
            await PackingRepository.AddOrUpdateAsync(_currentActivity);
        }
        catch (Exception ex)
        {
            ToastService.ShowError(string.Format(AppResources.FailedToUpdateItem, ex.Message));
        }
    }

    private void OnKeyboardVisibilityChanged(bool isVisible, double height)
    {
        _keyboardVisible = isVisible;
        _keyboardHeight = height;

        if (isVisible)
        {
            // Adjust page padding to account for keyboard height
            _ = JSRuntime.InvokeVoidAsync("adjustPageForKeyboard", height);
            
            // Scroll the active input into view only if covered by the keyboard
            _ = JSRuntime.InvokeVoidAsync("scrollActiveElementIntoView", height);
        }
        else
        {
            // Remove padding when keyboard hides
            _ = JSRuntime.InvokeVoidAsync("adjustPageForKeyboard", 0);
        }
        
        // Trigger UI update to reposition quick-add-container
        StateHasChanged();
    }

    private string GetBulkAddPlaceholder()
    {
        return AppResources.ResourceManager.GetString("BulkAddPlaceholder", LocalizationService.CurrentCulture) 
            ?? "Enter items (one per line):\nToothbrush\nToothpaste\nShampoo";
    }

    private async Task PromptDelete(PackingItemView item)
    {
        if (string.IsNullOrWhiteSpace(Id))
            return;

        try
        {
            await PackingRepository.DeleteItemAsync(item.Item.Id);
            await LoadItemsForPackingAsync(Id);
        }
        catch (Exception ex)
        {
            ToastService.ShowError(string.Format(AppResources.FailedToDeleteItem, ex.Message));
        }
    }

    private async Task CopyPacking()
    {
        await CloseOverflowMenuAsync();
        if (string.IsNullOrWhiteSpace(Id))
            return;

        try
        {
            var newId = await PackingRepository.CopyPackingAsync(Id);
            ToastService.ShowSuccess(AppResources.PackingActivityCopied);
            Navigation.NavigateTo($"/edit-packing/{newId}");
        }
        catch (Exception ex)
        {
            ToastService.ShowError(string.Format(AppResources.FailedToCopyActivity, ex.Message));
        }
    }

    private async Task PromptDeleteActivity()
    {
        await CloseOverflowMenuAsync();
        bool confirm = await DialogService.ShowConfirmAsync(
            AppResources.DeleteActivity, 
            AppResources.DeleteActivityConfirm,
            AppResources.Delete,
            AppResources.Cancel);
            
        if (confirm)
        {
            await ConfirmDeleteActivityAsync();
        }
    }

    private async Task ConfirmDeleteActivityAsync()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            ToastService.ShowError("Activity ID is empty");
            return;
        }

        try
        {
            // Verify activity exists before deleting
            var activity = await PackingRepository.GetByIdAsync(Id);
            if (activity == null)
            {
                ToastService.ShowError($"Activity with ID '{Id}' not found");
                return;
            }

            // Perform the delete
            await PackingRepository.DeleteAsync(Id);

            // Set flag to prevent NavigationLock from re-saving the deleted activity
            _isDeleting = true;

            // Show success message
            ToastService.ShowSuccess(AppResources.ActivityDeleted ?? "Activity deleted successfully");

            // Navigate back
            Navigation.NavigateTo("/packing-activities");
        }
        catch (Exception ex)
        {
            ToastService.ShowError(string.Format(AppResources.FailedToDeleteActivity, ex.Message));
        }
    }

    private async Task SaveActivityNameAsync()
    {
        if (string.IsNullOrWhiteSpace(Id))
            return;

        // Trim the activity name
        _activityName = _activityName?.Trim() ?? string.Empty;

        // Don't save if name is empty or unchanged
        if (string.IsNullOrWhiteSpace(_activityName) || _activityName == _currentActivity?.Name)
            return;

        try
        {
            _currentActivity.Name = _activityName;
            await PackingRepository.AddOrUpdateAsync(_currentActivity);
            NavigationHeaderService.SetText(_activityName);
        }
        catch (Exception ex)
        {
            ToastService.ShowError(string.Format(AppResources.FailedToUpdateItem, ex.Message));
            // Revert to original name on error
            _activityName = _currentActivity?.Name ?? AppResources.NewPackingActivity;
            StateHasChanged();
        }
    }

    // Drag & drop handlers
    private void OnDragStart(DragEventArgs e, PackingItemView item)
    {
        _draggedItem = item;
        _dragOverItem = null;
        _isDragging = true;
        StateHasChanged();

        try
        {
            if (e.DataTransfer is not null)
            {
                e.DataTransfer.DropEffect = "move";
                e.DataTransfer.EffectAllowed = "move";
            }
            
            // Add class to body for dimming effect
            _ = JSRuntime.InvokeVoidAsync("document.body.classList.add", "dragging-active");
        }
        catch
        {
        }
    }

    private void OnDragEnd(DragEventArgs e, PackingItemView item)
    {
        try
        {
            // Clean up: remove dragging class from body
            _ = JSRuntime.InvokeVoidAsync("document.body.classList.remove", "dragging-active");
        }
        catch { }

        // Reset drag state
        _draggedItem = null;
        _dragOverItem = null;
        _isDragging = false;
        StateHasChanged();
    }

    private async Task OnDragOver(DragEventArgs e, PackingItemView item)
    {
        try
        {
            if (e.DataTransfer is not null)
                e.DataTransfer.DropEffect = "move";
            
            // Update drop line position based on mouse Y coordinate
            if (_draggedItem != null && !ReferenceEquals(_draggedItem, item) && e.ClientY > 0)
            {
                var itemId = $"item-{item.Item.Id}";
                var position = await JSRuntime.InvokeAsync<string>("getDropLinePosition", itemId, e.ClientY);
                
                if (_dropLinePosition != position)
                {
                    _dropLinePosition = position;
                    StateHasChanged();
                }
            }
        }
        catch { }
    }

    private void OnDragEnter(DragEventArgs e, PackingItemView item)
    {
        if (_draggedItem != null && !ReferenceEquals(_draggedItem, item))
        {
            _dragOverItem = item;
            // Position will be set by OnDragOver
            if (string.IsNullOrEmpty(_dropLinePosition))
            {
                _dropLinePosition = "after";
            }
            StateHasChanged();
        }
    }

    private void OnDragLeave(DragEventArgs e, PackingItemView item)
    {
        if (ReferenceEquals(_dragOverItem, item))
        {
            _dragOverItem = null;
            _dropLinePosition = "";
            StateHasChanged();
        }
    }

    private string GetPackingRowClass(PackingItemView item)
    {
        var baseClass = "packing-row";
        if (ReferenceEquals(item, _draggedItem))
            return $"{baseClass} dragging";
        if (ReferenceEquals(item, _dragOverItem))
            return $"{baseClass} drag-over drop-line-{_dropLinePosition}";
        if (ReferenceEquals(item, _editingItem))
            return $"{baseClass} editing";
        return baseClass;
    }

    private async Task OnDrop(DragEventArgs e, PackingItemView target)
    {
        try
        {
            // Remove dragging class from body
            _ = JSRuntime.InvokeVoidAsync("document.body.classList.remove", "dragging-active");
        }
        catch { }

        if (_draggedItem is null)
            return;

        var dragged = _draggedItem;
        if (ReferenceEquals(dragged, target))
        {
            _draggedItem = null;
            _dragOverItem = null;
            _dropLinePosition = "";
            return;
        }

        var oldCategory = dragged.Item.Category;
        var categoryChanged = !string.Equals(dragged.Item.Category, target.Item.Category, StringComparison.OrdinalIgnoreCase);

        Items.Remove(dragged);

        if (string.Equals(dragged.Item.Category, target.Item.Category, StringComparison.OrdinalIgnoreCase))
        {
            var targetIndex = Items.IndexOf(target);
            if (targetIndex < 0)
            {
                Items.Add(dragged);
            }
            else
            {
                // Respect the drop line position (before/after)
                if (_dropLinePosition == "before")
                {
                    Items.Insert(targetIndex, dragged);
                }
                else
                {
                    Items.Insert(targetIndex + 1, dragged);
                }
            }
        }
        else
        {
            dragged.Item.Category = target.Item.Category;

            var lastIndex = Items.FindLastIndex(i => string.Equals(i.Item.Category, target.Item.Category, StringComparison.OrdinalIgnoreCase));
            if (lastIndex < 0)
            {
                Items.Add(dragged);
            }
            else
            {
                Items.Insert(lastIndex + 1, dragged);
            }

            if (!Items.Any(i => string.Equals(i.Item.Category, oldCategory, StringComparison.OrdinalIgnoreCase)))
            {
                _categoryOrder.RemoveAll(c => string.Equals(c, oldCategory, StringComparison.OrdinalIgnoreCase));
                _manualOverride.Remove(oldCategory);
            }

            if (!_categoryOrder.Any(c => string.Equals(c, target.Item.Category, StringComparison.OrdinalIgnoreCase)))
            {
                _categoryOrder.Add(target.Item.Category);
                _manualOverride[target.Item.Category] = null;
            }
        }

        _manualOverride[target.Item.Category] = false;

        _draggedItem = null;
        _dragOverItem = null;
        _dropLinePosition = "";
        _isDragging = false;

        // Show toast notification for category changes
        if (categoryChanged)
        {
            var localizedNewCategory = GetLocalizedCategoryName(target.Item.Category);
        }

        // Update sort order for all items and persist to database
        await UpdateAndSaveItemsOrder();

        StateHasChanged();
    }

    private bool _isSavingBeforeNav;
    private bool _isDeleting;

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        if (_isSavingBeforeNav)
            return;

        // Don't auto-save if we're deleting the activity
        if (_isDeleting)
            return;

        if (string.IsNullOrWhiteSpace(Id))
            return;

        _isSavingBeforeNav = true;
        try
        {
            context.PreventNavigation();

            // Save activity details
            await PackingRepository.AddOrUpdateAsync(_currentActivity);

            // Save items order before navigating
            await UpdateAndSaveItemsOrder();

            Navigation.NavigateTo(context.TargetLocation);
        }
        finally
        {
            _isSavingBeforeNav = false;
        }
    }

    private void ToggleCategorySelector()
    {
        _showCategorySelector = !_showCategorySelector;
    }

    private void SelectCategory(PackingCategory category)
    {
        _showCategorySelector = false;
    }

    private string GetCategoryIcon(PackingCategory category)
    {
        return CategoryIconProvider.GetIcon(category);
    }

    private string GetLocalizedCategoryName(PackingCategory category)
    {
        var resourceKey = LocalizationService.GetLocalizedCategory(category.ToString());
        return AppResources.ResourceManager.GetString(resourceKey, LocalizationService.CurrentCulture) ?? category.ToString();
    }

    private string GetLocalizedCategoryName(string categoryString)
    {
        if (Enum.TryParse<PackingCategory>(categoryString, out var category))
        {
            return GetLocalizedCategoryName(category);
        }
        return categoryString;
    }

    private async Task StartEditingItem(PackingItemView item)
    {
        // Don't trigger edit if we're currently dragging
        if (_isDragging)
            return;

        if (_isAddingItem)
            await CancelAdd();

        if (_editingItem is not null && !ReferenceEquals(_editingItem, item))
        {
            var currentEdit = _editingItem;
            if (HasEditingChanges(currentEdit))
                await SaveItemChanges(currentEdit);
            else
                await CancelItemEdit(currentEdit);
        }

        _editingItem = item;
        _editingOriginalName = item.Item.Name;
        _editingOriginalNotes = item.Item.Notes;
        StateHasChanged();

        await SyncClickOutsideHandlerAsync();

        // Delay to ensure DOM is ready and rendered
        await Task.Delay(100);

        // Focus on the edit input (keyboard handler will scroll if needed)
        try
        {
            await JSRuntime.InvokeVoidAsync("focusElement", itemEditInputElement);
        }
        catch
        {
            // ignore - element may not be present in DOM
        }
    }
    
    private async Task SelectAllInputText(FocusEventArgs e)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("selectAllText", itemEditInputElement);
        }
        catch
        {
            // ignore
        }
    }

    private void UpdateItemName(PackingItemView item, string? newName)
    {
        if (newName != null)
        {
            item.Item.Name = newName;
        }
    }

    private async Task SaveItemChanges(PackingItemView item)
    {
        if (string.IsNullOrWhiteSpace(Id))
            return;

        try
        {
            if (string.IsNullOrWhiteSpace(item.Item.Name))
            {
                item.Item.Name = AppResources.UnnamedItem;
            }

            await PackingRepository.AddOrUpdateItemAsync(item.Item);
        }
        catch (Exception ex)
        {
            ToastService.ShowError(string.Format(AppResources.FailedToUpdateItem, ex.Message));
        }
        finally
        {
            _editingItem = null;
            _editingOriginalName = null;
            _editingOriginalNotes = null;
            StateHasChanged();
            await SyncClickOutsideHandlerAsync();
        }
    }

    private void SwitchToPackingMode()
    {
        if (!string.IsNullOrEmpty(Id))
        {
            Navigation.NavigateTo($"/play-packing/{Id}");
        }
    }

    private void HandleItemEditKeyDown(KeyboardEventArgs e, PackingItemView item)
    {
        if (e.Key == "Escape")
        {
            CancelItemEdit(item);
        }
        else if (e.Key == "Enter" && !e.ShiftKey)
        {
            _ = SaveItemChanges(item);
        }
    }

    private async Task CancelItemEdit(PackingItemView item)
    {
        _editingItem = null;
        _editingOriginalName = null;
        _editingOriginalNotes = null;
        StateHasChanged();
        await SyncClickOutsideHandlerAsync();
    }

    private bool HasEditingChanges(PackingItemView item)
    {
        var originalName = _editingOriginalName ?? string.Empty;
        var originalNotes = _editingOriginalNotes ?? string.Empty;

        var currentName = item.Item.Name ?? string.Empty;
        var currentNotes = item.Item.Notes ?? string.Empty;

        return !string.Equals(originalName, currentName, StringComparison.Ordinal)
            || !string.Equals(originalNotes, currentNotes, StringComparison.Ordinal);
    }

    private async Task CloseFlyoutsByClickOffAsync()
    {
        if (_isAddingItem)
        {
            await CancelAdd();
        }

        if (_editingItem is not null)
        {
            var item = _editingItem;
            if (HasEditingChanges(item))
            {
                await SaveItemChanges(item);
            }
            else
            {
                await CancelItemEdit(item);
            }
        }
    }

    [JSInvokable]
    public async Task CloseFlyoutsByClickOffJsAsync()
    {
        await CloseFlyoutsByClickOffAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task SyncClickOutsideHandlerAsync()
    {
        if (_editPackingModule is null)
            return;
        try
        {
            if (_isAddingItem || _editingItem is not null)
            {
                _dotNetHelper ??= DotNetObjectReference.Create(this);
                await _editPackingModule.InvokeVoidAsync("registerClickOutsideHandler", _dotNetHelper);
            }
            else
            {
                await _editPackingModule.InvokeVoidAsync("unregisterClickOutsideHandler");
            }
        }
        catch { }
    }

    private void HandleBulkTextareaKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && (e.CtrlKey || e.MetaKey))
        {
            _ = ConfirmAddAsync();
        }
    }

    private async Task HandleTextareaAutoGrow()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("autoGrowTextarea", bulkItemsTextarea);
        }
        catch
        {
            // Silently fail if JS function not available
        }
    }

    private async Task UpdateAndSaveItemsOrder()
    {
        // Update SortOrder for all items based on their current position in the Items list
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].Item.SortOrder = i;
        }

        // Save all items to database with their new sort order
        if (!string.IsNullOrWhiteSpace(Id))
        {
            try
            {
                await PackingRepository.UpdateItemsSortOrderAsync(Items.Select(x => x.Item));
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Failed to save item order: {ex.Message}");
            }
        }
    }

    // Speech recognition methods
    private async Task ToggleSpeechRecognition()
    {
        if (!_isSpeechRecognitionSupported)
        {
            ToastService.ShowError(AppResources.SpeechNotSupported);
            return;
        }

        if (_isListening)
        {
            await StopSpeechRecognition();
        }
        else
        {
            await StartSpeechRecognition();
        }
    }

    private async Task StartSpeechRecognition()
    {
        try
        {
            // Request microphone permission before starting speech recognition
            var hasPermission = await PermissionBridge.RequestPermissionAsync();
            
            if (!hasPermission)
            {
                ToastService.ShowError($"{AppResources.MicrophonePermissionDenied}. {AppResources.MicrophonePermissionHelp}");
                _isListening = false;
                StateHasChanged();
                return;
            }

            _dotNetHelper = DotNetObjectReference.Create(this);
            
            // Get the speech recognition language based on current app language
            var speechLanguage = SpeechRecognitionLanguageHelper.GetSpeechRecognitionLanguage(LocalizationService.CurrentCulture);
            
            var initResult = await JSRuntime.InvokeAsync<object>("initializeSpeechRecognition", bulkItemsTextarea, _dotNetHelper, speechLanguage);
            
            // Note: startSpeechRecognition is now async in JavaScript
            var startResult = await JSRuntime.InvokeAsync<JsonElement>("startSpeechRecognition");
            
            // Check if permission was denied at browser level
            if (startResult.TryGetProperty("needsPermission", out var needsPermission) && needsPermission.GetBoolean())
            {
                ToastService.ShowError($"{AppResources.MicrophonePermissionDenied}. {AppResources.MicrophonePermissionHelp}");
                _isListening = false;
                StateHasChanged();
            }
            // Result handling will be done through callbacks
        }
        catch (Exception ex)
        {
            ToastService.ShowError(string.Format(AppResources.SpeechError, ex.Message));
            _isListening = false;
            StateHasChanged();
        }
    }

    private async Task StopSpeechRecognition()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("stopSpeechRecognition");
            _isListening = false;
            _interimTranscript = string.Empty;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError(string.Format(AppResources.SpeechError, ex.Message));
        }
    }

    [JSInvokable]
    public void OnSpeechRecognitionStarted()
    {
        _isListening = true;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnSpeechRecognitionStopped()
    {
        _isListening = false;
        _interimTranscript = string.Empty;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnSpeechRecognitionResult(string finalTranscript, string interimTranscript)
    {
        // Update textbox in real-time with final transcript
        _bulkItemsText = finalTranscript;
        _interimTranscript = interimTranscript;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnSpeechRecognitionError(string error)
    {
        _isListening = false;
        _interimTranscript = string.Empty;
        
        var errorMessage = error switch
        {
            "not-allowed" or "permission-denied" => $"{AppResources.MicrophonePermissionDenied}. {AppResources.MicrophonePermissionHelp}",
            "not-supported" => AppResources.SpeechNotSupported,
            "no-speech" => AppResources.NoSpeechDetected,
            "audio-capture" => AppResources.AudioCaptureError,
            "network" => AppResources.NetworkErrorSpeech,
            "language-not-supported" => AppResources.LanguageNotSupported,
            _ => string.Format(AppResources.SpeechError, error)
        };

        ToastService.ShowError(errorMessage);
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (KeyboardService != null)
            {
                KeyboardService.KeyboardVisibilityChanged -= OnKeyboardVisibilityChanged;
            }

            // Stop speech recognition if active
            if (_isListening)
            {
                await JSRuntime.InvokeVoidAsync("stopSpeechRecognition");
            }

            if (_editPackingModule is not null)
            {
                await _editPackingModule.InvokeVoidAsync("unregisterClickOutsideHandler");
                await _editPackingModule.InvokeVoidAsync("unregisterOverflowClickOutside");
                await _editPackingModule.DisposeAsync();
            }

            // Dispose DotNetObjectReference
            _dotNetHelper?.Dispose();
        }
        catch { }
    }
}
