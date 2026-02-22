using Anticipack.Components.Shared.NavigationHeaderComponent;
using Anticipack.Resources.Localization;
using Anticipack.Services;
using Anticipack.Services.Categories;
using Anticipack.Services.Statistics;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Anticipack.Components.Features.Statistics;

public partial class PackingStatistics : IDisposable
{
    [Inject] private IPackingStatisticsService StatisticsService { get; set; } = default!;
    [Inject] private IStringLocalizer<AppResources> Localizer { get; set; } = default!;
    [Inject] private INavigationHeaderService NavigationHeaderService { get; set; } = default!;
    [Inject] private ILocalizationService LocalizationService { get; set; } = default!;
    [Inject] private ICategoryIconProvider CategoryIconProvider { get; set; } = default!;

    private PackingStatisticsData? _stats;
    private bool _isLoading = true;

    protected override void OnInitialized()
    {
        NavigationHeaderService.SetText(Localizer["StatsTitle"]);
        LocalizationService.CultureChanged += OnCultureChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadStatisticsAsync();
    }

    private async Task LoadStatisticsAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            _stats = await StatisticsService.GetStatisticsAsync();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private void OnCultureChanged(object? sender, System.Globalization.CultureInfo culture)
    {
        InvokeAsync(StateHasChanged);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes:D2}m";
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes}m {duration.Seconds:D2}s";
        return $"{(int)duration.TotalSeconds}s";
    }

    private static string FormatTotalDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes:D2}m";
        return $"{(int)duration.TotalMinutes}m";
    }

    private int GetBarHeight(int durationSeconds)
    {
        if (_stats is null || _stats.RecentSessionDurations.Count == 0)
            return 0;

        var max = _stats.RecentSessionDurations.Max(p => p.DurationSeconds);
        if (max == 0)
            return 0;

        return Math.Max(8, (int)((double)durationSeconds / max * 100));
    }

    private string GetCategoryIcon(string category)
    {
        return "fa " + CategoryIconProvider.GetIcon(category);
    }

    private string GetLocalizedCategory(string category)
    {
        var key = $"Category_{category}";
        var localized = Localizer[key];
        return localized.ResourceNotFound ? category : localized.Value;
    }

    private string GetDayName(DayOfWeek day)
    {
        return System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(day);
    }

    public void Dispose()
    {
        LocalizationService.CultureChanged -= OnCultureChanged;
    }
}
