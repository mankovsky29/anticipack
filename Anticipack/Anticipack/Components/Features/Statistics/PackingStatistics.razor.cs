using Anticipack.Components.Shared.DialogComponent;
using Anticipack.Components.Shared.NavigationHeaderComponent;
using Anticipack.Components.Shared.ToastComponent;
using Anticipack.Resources.Localization;
using Anticipack.Services;
using Anticipack.Services.Statistics;
using Anticipack.Storage;
using Anticipack.Storage.Repositories;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Anticipack.Components.Features.Statistics;

public partial class PackingStatistics : IDisposable
{
    [Inject] private IPackingStatisticsService StatisticsService { get; set; } = default!;
    [Inject] private IPackingActivityRepository ActivityRepository { get; set; } = default!;
    [Inject] private IStringLocalizer<AppResources> Localizer { get; set; } = default!;
    [Inject] private INavigationHeaderService NavigationHeaderService { get; set; } = default!;
    [Inject] private ILocalizationService LocalizationService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    private PackingInsights? _insights;
    private bool _isLoading = true;

    protected override void OnInitialized()
    {
        NavigationHeaderService.SetText(Localizer["StatsTitle"]);
        LocalizationService.CultureChanged += OnCultureChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadInsightsAsync();
    }

    private async Task LoadInsightsAsync()
    {
        try
        {
            _insights = await StatisticsService.GetInsightsAsync();
        }
        catch
        {
            ToastService.ShowError(Localizer["FailedToLoad"]);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void OpenForPacking(PackingActivity activity)
    {
        Navigation.NavigateTo($"/play-packing/{activity.Id}");
    }

    private void OpenForEditing(PackingActivity activity)
    {
        Navigation.NavigateTo($"/edit-packing/{activity.Id}");
    }

    private async Task ArchiveAsync(PackingActivity activity)
    {
        try
        {
            activity.IsArchived = true;
            await ActivityRepository.AddOrUpdateAsync(activity);
            ToastService.ShowSuccess(string.Format(Localizer["ActivityArchived"], activity.Name));
            await LoadInsightsAsync();
        }
        catch
        {
            activity.IsArchived = false;
            ToastService.ShowError(Localizer["FailedToUpdateArchiveStatus"]);
        }
    }

    private async Task DeleteAsync(PackingActivity activity)
    {
        var confirmed = await DialogService.ShowConfirmAsync(
            Localizer["DeleteActivityTitle"],
            string.Format(Localizer["DeleteActivityMessage"], activity.Name));

        if (!confirmed)
            return;

        try
        {
            await ActivityRepository.DeleteAsync(activity.Id);
            ToastService.ShowSuccess(Localizer["ActivityDeleted"]);
            await LoadInsightsAsync();
        }
        catch
        {
            ToastService.ShowError(Localizer["FailedToDelete"]);
        }
    }

    private string FormatDate(DateTime date)
    {
        return date.ToString("d MMM yyyy", LocalizationService.CurrentCulture);
    }

    private void OnCultureChanged(object? sender, System.Globalization.CultureInfo culture)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        LocalizationService.CultureChanged -= OnCultureChanged;
    }
}
