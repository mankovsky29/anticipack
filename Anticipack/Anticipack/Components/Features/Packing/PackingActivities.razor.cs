using Anticipack.Components.Shared.DialogComponent;
using Anticipack.Components.Shared.NavigationHeaderComponent;
using Anticipack.Components.Shared.ToastComponent;
using Anticipack.Resources.Localization;
using Anticipack.Services;
using Anticipack.Services.Categories;
using Anticipack.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Microsoft.Maui.Storage;

namespace Anticipack.Components.Features.Packing;

public partial class PackingActivities : IAsyncDisposable
{
    private const string OnboardingSeenPreferenceKey = "ux.onboarding.seen.v1";

    [Inject] private IPackingRepository PackingRepository { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ILocalizationService LocalizationService { get; set; } = default!;
    [Inject] private IStringLocalizer<AppResources> Localizer { get; set; } = default!;
    [Inject] private INavigationHeaderService NavigationHeaderService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IKeyboardService KeyboardService { get; set; } = default!;
    [Inject] private ICategoryIconProvider CategoryIconProvider { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "create")]
    public bool? Create { get; set; }

    private enum ArchiveFilterType
    {
        Active,
        Archived
    }

    private List<Storage.PackingActivity> _activities = new();
    private Dictionary<string, int> _activityItemCounts = new();
    private Dictionary<string, int> _activityPackedCounts = new();
    private Dictionary<string, List<string>> _activityTopCategories = new();
    private bool _isLoading = true;
    private string _searchTerm = string.Empty;
    private ArchiveFilterType _archiveFilter = ArchiveFilterType.Active;
    private string? _openMenuId;
    private bool _menuJustOpened;
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<PackingActivities>? _dotNetRef;

    private bool _isCreating = false;

    // Scroll to top
    private bool _showScrollToTop;
    private bool _showOnboardingCard;
    private bool _hasHandledCreateQuery;
    
    // Keyboard
    private bool _keyboardVisible;
    private double _keyboardHeight;

    protected override void OnInitialized()
    {
        LocalizationService.CultureChanged += OnCultureChanged;
        KeyboardService.KeyboardVisibilityChanged += OnKeyboardVisibilityChanged;
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            KeyboardService.Initialize(this);
            await JSRuntime.InvokeVoidAsync("initScrollToTopButton", _dotNetRef);
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./Components/Features/Packing/PackingActivities.razor.js");
        }

        if (_menuJustOpened && _jsModule is not null)
        {
            _menuJustOpened = false;
            try
            {
                await _jsModule.InvokeVoidAsync("positionMenu");
            }
            catch { }
        }
        
    }
    
    [JSInvokable]
    public void UpdateScrollToTopVisibility(bool show)
    {
        if (_showScrollToTop != show)
        {
            _showScrollToTop = show;
            StateHasChanged();
        }
    }
    
    private async Task ScrollToTopAsync()
    {
        await JSRuntime.InvokeVoidAsync("scrollToTop");
    }

    protected override async Task OnInitializedAsync()
    {
        NavigationHeaderService.SetText(Localizer["PackingActivitiesMenu"]);
        await LoadActivitiesAsync();

        _showOnboardingCard = !_activities.Any() && !Preferences.Default.Get(OnboardingSeenPreferenceKey, false);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Create == true && !_hasHandledCreateQuery)
        {
            _hasHandledCreateQuery = true;
            await AddNewActivityAsync();
            Navigation.NavigateTo("/packing-activities", replace: true);
        }
    }

    private void OnCultureChanged(object? sender, System.Globalization.CultureInfo culture)
    {
        InvokeAsync(StateHasChanged);
    }

    private void ClearSearch()
    {
        _searchTerm = string.Empty;
    }

    private void SetArchiveFilter(ArchiveFilterType filterType)
    {
        _archiveFilter = filterType;
    }

    private async Task ToggleMenuAsync(string activityId)
    {
        if (_openMenuId == activityId)
        {
            _openMenuId = null;
            if (_jsModule is not null)
                try { await _jsModule.InvokeVoidAsync("unregisterMenuClickOutside"); } catch { }
        }
        else
        {
            _openMenuId = activityId;
            _menuJustOpened = true;
            if (_jsModule is not null && _dotNetRef is not null)
                try { await _jsModule.InvokeVoidAsync("registerMenuClickOutside", _dotNetRef); } catch { }
        }
    }

    [JSInvokable]
    public async Task CloseMenuFromJsAsync()
    {
        _openMenuId = null;
        if (_jsModule is not null)
            try { await _jsModule.InvokeVoidAsync("unregisterMenuClickOutside"); } catch { }
        StateHasChanged();
    }

    private async Task CloseMenuAsync()
    {
        _openMenuId = null;
        if (_jsModule is not null)
            try { await _jsModule.InvokeVoidAsync("unregisterMenuClickOutside"); } catch { }
    }

    private async Task LoadActivitiesAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            _activities = await PackingRepository.GetAllAsync();
            _activities = _activities.OrderByDescending(a => a.LastPacked).ThenBy(a => a.RunCount).ToList();

            // Load metadata for each activity
            _activityItemCounts.Clear();
            _activityPackedCounts.Clear();
            _activityTopCategories.Clear();

            foreach (var activity in _activities)
            {
                var items = await PackingRepository.GetItemsForActivityAsync(activity.Id);
                _activityItemCounts[activity.Id] = items.Count;
                _activityPackedCounts[activity.Id] = items.Count(i => i.IsPacked);
                
                // Get top categories by count
                var topCategories = items
                    .GroupBy(i => i.Category)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();
                    
                _activityTopCategories[activity.Id] = topCategories;
            }
        }
        catch
        {
            ToastService.ShowError(Localizer["FailedToLoad"]);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private IEnumerable<Storage.PackingActivity> FilteredActivities
    {
        get
        {
            var filtered = _activities.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(_searchTerm))
            {
                filtered = filtered.Where(a => a.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            // Apply archive filter
            filtered = _archiveFilter switch
            {
                ArchiveFilterType.Active => filtered.Where(a => !a.IsArchived),
                ArchiveFilterType.Archived => filtered.Where(a => a.IsArchived),
                _ => filtered
            };
            
            return filtered;
        }
    }

    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;
        
        if (timeSpan.TotalMinutes < 1)
            return Localizer["JustNow"];
        if (timeSpan.TotalHours < 1)
            return string.Format(Localizer["MinutesAgo"], (int)timeSpan.TotalMinutes);
        if (timeSpan.TotalDays < 1)
            return string.Format(Localizer["HoursAgo"], (int)timeSpan.TotalHours);
        if (timeSpan.TotalDays < 7)
            return string.Format(Localizer["DaysAgo"], (int)timeSpan.TotalDays);
        if (timeSpan.TotalDays < 30)
            return string.Format(Localizer["WeeksAgo"], (int)(timeSpan.TotalDays / 7));
        if (timeSpan.TotalDays < 365)
            return string.Format(Localizer["MonthsAgo"], (int)(timeSpan.TotalDays / 30));
        
        return dateTime.ToString("MMM d, yyyy");
    }

    private string GetItemsText()
    {
        return Localizer["Items"].Value.ToString().ToLower();
    }

    private string GetCategoryIcon(string category)
    {
        return "fa " + CategoryIconProvider.GetIcon(category);
    }

    private async Task AddNewActivityAsync()
    {
        if (_isCreating)
            return;

        if (_showOnboardingCard)
        {
            Preferences.Default.Set(OnboardingSeenPreferenceKey, true);
            _showOnboardingCard = false;
        }

        _isCreating = true;

        var createdAt = DateTime.Now;
        var newActivity = new Storage.PackingActivity
        {
            Name = createdAt.ToString("yyyy-MM-dd:hh:mm"),
            LastPacked = createdAt,
            IsShared = false,
            IsRecurring = false
        };

        try
        {
            await PackingRepository.AddOrUpdateAsync(newActivity);
            ToastService.ShowSuccess(Localizer["ActivityCreated"]);
            Navigation.NavigateTo($"/packing-activity?id={newActivity.Id}&mode=edit");
        }
        catch
        {
            ToastService.ShowError(Localizer["FailedToCreate"]);
        }
        finally
        {
            _isCreating = false;
        }
    }

    private void DismissOnboarding()
    {
        Preferences.Default.Set(OnboardingSeenPreferenceKey, true);
        _showOnboardingCard = false;
    }

    private async Task ToggleArchiveActivity(Storage.PackingActivity activity)
    {
        await CloseMenuAsync();
        var newArchiveState = !activity.IsArchived;
        var confirmMessage = newArchiveState 
            ? string.Format(Localizer["ConfirmArchive"], activity.Name)
            : string.Format(Localizer["ConfirmUnarchive"], activity.Name);
        
        var result = await DialogService.ShowConfirmAsync(
            Localizer["ChangeArchiveStatus"], 
            confirmMessage,
            Localizer["Confirm"],
            Localizer["Cancel"]);
        
        if (!result) 
            return;
            
        activity.IsArchived = newArchiveState;
        
        try
        {
            await PackingRepository.AddOrUpdateAsync(activity);
            var message = activity.IsArchived 
                ? string.Format(Localizer["ActivityArchived"], activity.Name)
                : string.Format(Localizer["ActivityUnarchived"], activity.Name);
            ToastService.ShowSuccess(message);
            await LoadActivitiesAsync();
        }
        catch
        {
            ToastService.ShowError(Localizer["FailedToUpdateArchiveStatus"]);
        }
    }

    private async Task PromptDeleteActivity(Storage.PackingActivity activity)
    {
        await CloseMenuAsync();
        var message = string.Format(Localizer["DeleteActivityMessage"], activity.Name);
        var result = await DialogService.ShowConfirmAsync(Localizer["DeleteActivityTitle"], message);
        
        if (!result) 
            return;
            
        await DeleteActivityAsync(activity);
    }

    private async Task DeleteActivityAsync(Storage.PackingActivity activity)
    {
        try
        {
            await PackingRepository.DeleteAsync(activity.Id);
            ToastService.ShowSuccess(Localizer["ActivityDeleted"]);
            await LoadActivitiesAsync();
        }
        catch
        {
            ToastService.ShowError(Localizer["FailedToDelete"]);
        }
    }

    private void OpenActivityInPackingMode(string id)
    {
        Navigation.NavigateTo($"/packing-activity?id={id}&mode=packing");
    }

    private async Task OpenActivityInEditMode(string id)
    {
        await CloseMenuAsync();
        Navigation.NavigateTo($"/packing-activity?id={id}&mode=edit");
    }

    private void OnKeyboardVisibilityChanged(bool isVisible, double height)
    {
        _keyboardVisible = isVisible;
        _keyboardHeight = height;

        if (isVisible)
        {
            _ = JSRuntime.InvokeVoidAsync("adjustPageForKeyboard", height);
            _ = JSRuntime.InvokeVoidAsync("scrollActiveElementIntoView", height);
        }
        else
        {
            _ = JSRuntime.InvokeVoidAsync("adjustPageForKeyboard", 0);
        }
    }

    public async ValueTask DisposeAsync()
    {
        LocalizationService.CultureChanged -= OnCultureChanged;
        KeyboardService.KeyboardVisibilityChanged -= OnKeyboardVisibilityChanged;

        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("unregisterMenuClickOutside");
                await _jsModule.DisposeAsync();
            }
            catch { }
        }

        _dotNetRef?.Dispose();
    }
}
