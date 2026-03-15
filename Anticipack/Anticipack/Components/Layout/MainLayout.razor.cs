using Anticipack.Components.Shared.DialogComponent;
using Anticipack.Resources.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Networking;

namespace Anticipack.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private bool _dialogVisible;
    private DialogOptions _dialogOptions = new();
    private bool _showStoragePermissionBanner;
    private bool _isOffline;

    private string PermissionBannerTitle => AppResources.ResourceManager.GetString("StoragePermissionRequiredTitle") ?? "Permission required";
    private string PermissionBannerMessage => AppResources.ResourceManager.GetString("StoragePermissionRequiredMessage") ?? "Storage permission is required to save and load your packing data. You can enable it later in system settings.";
    private string OpenSettingsText => AppResources.ResourceManager.GetString("OpenSettings") ?? "Open Settings";
    private string RetryText => AppResources.ResourceManager.GetString("Retry") ?? "Retry";
    private string OfflineNotice => AppResources.ResourceManager.GetString("OfflineChangesSaved") ?? "Offline mode: your changes are saved locally.";

    protected override void OnInitialized()
    {
        if (DialogService is DialogService service)
        {
            service.OnDialogShow += ShowDialog;
            service.OnDialogClose += CloseDialog;
        }

        _isOffline = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        _ = CheckStoragePermissionAsync();
    }

    private async Task CheckStoragePermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
        _showStoragePermissionBanner = status != PermissionStatus.Granted;
        await InvokeAsync(StateHasChanged);
    }

    private async Task RetryStoragePermissionAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
        _showStoragePermissionBanner = status != PermissionStatus.Granted;
    }

    private void OpenAppSettings()
    {
        AppInfo.ShowSettingsUI();
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        _isOffline = e.NetworkAccess != NetworkAccess.Internet;
        InvokeAsync(StateHasChanged);
    }

    private void ShowDialog(DialogOptions options)
    {
        _dialogOptions = options;
        _dialogVisible = true;
        StateHasChanged();
    }

    private void CloseDialog()
    {
        _dialogVisible = false;
        StateHasChanged();
    }

    private void HandleDialogClose()
    {
        if (_dialogVisible && _dialogOptions.OnConfirmCallback is not null)
        {
            _dialogOptions.OnConfirmCallback.Invoke(false);
        }

        _dialogVisible = false;
        StateHasChanged();
    }

    private void HandleDialogConfirm(bool result)
    {
        _dialogOptions.OnConfirmCallback?.Invoke(result);
        _dialogVisible = false;
        StateHasChanged();
    }

    public void Dispose()
    {
        if (DialogService is DialogService service)
        {
            service.OnDialogShow -= ShowDialog;
            service.OnDialogClose -= CloseDialog;
        }

        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }
}
