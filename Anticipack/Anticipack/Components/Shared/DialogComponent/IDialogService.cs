using Microsoft.AspNetCore.Components;

namespace Anticipack.Components.Shared.DialogComponent
{
    public interface IDialogService
    {
        Task ShowAlertAsync(string title, string message, string okText = "OK");

        Task<bool> ShowConfirmAsync(string title, string message,
            string confirmText = "Confirm", string cancelText = "Cancel");

        Task<bool> ShowConfirmAsync(string title, RenderFragment content,
            string confirmText = "Confirm", string cancelText = "Cancel");

        Task ShowCustomAsync(string title, RenderFragment content);

        Task CloseAsync();
    }
}
