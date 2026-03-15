using Anticipack.Components.Shared.DialogComponent;
using Microsoft.AspNetCore.Components;

namespace Anticipack.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private bool _dialogVisible;
    private DialogOptions _dialogOptions = new();

    protected override void OnInitialized()
    {
        if (DialogService is DialogService service)
        {
            service.OnDialogShow += ShowDialog;
            service.OnDialogClose += CloseDialog;
        }
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
    }
}
