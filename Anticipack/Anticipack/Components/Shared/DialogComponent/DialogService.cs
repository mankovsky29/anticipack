using Microsoft.AspNetCore.Components;

namespace Anticipack.Components.Shared.DialogComponent
{
    public class DialogService : IDialogService
    {
        // Events that the MainLayout will subscribe to
        public event Action<DialogOptions>? OnDialogShow;
        public event Action? OnDialogClose;

        // Track the current active task completion source
        private TaskCompletionSource<bool>? _currentConfirmTcs;

        public Task ShowAlertAsync(string title, string message, string okText = "OK")
        {
            var options = new DialogOptions
            {
                Title = title,
                Message = message,
                OkText = okText,
                DialogType = DialogType.Info,
                ShowCloseButton = true,
                OnConfirmCallback = _ => { } // Empty callback for alert
            };

            OnDialogShow?.Invoke(options);
            return Task.CompletedTask;
        }

        public Task<bool> ShowConfirmAsync(string title, string message,
            string confirmText = "Confirm", string cancelText = "Cancel")
        {
            // Cancel any previous task completion source
            _currentConfirmTcs?.TrySetCanceled();
            
            var tcs = new TaskCompletionSource<bool>();
            _currentConfirmTcs = tcs;

            var options = new DialogOptions
            {
                Title = title,
                Message = message,
                ConfirmText = confirmText,
                CancelText = cancelText,
                DialogType = DialogType.Info,
                OkText = string.Empty, // No OK button for confirm dialog
                ShowCloseButton = true,
                CloseOnOverlayClick = false, // Prevent accidentally closing
                OnConfirmCallback = result =>
                {
                    tcs.TrySetResult(result);
                    _currentConfirmTcs = null;
                }
            };

            OnDialogShow?.Invoke(options);
            return tcs.Task;
        }

        public Task ShowCustomAsync(string title, RenderFragment content)
        {
            var options = new DialogOptions
            {
                Title = title,
                ContentTemplate = content,
                DialogType = DialogType.Default,
                OkText = "OK",
                ShowCloseButton = true
            };

            OnDialogShow?.Invoke(options);
            return Task.CompletedTask;
        }

        public Task CloseAsync()
        {
            // If there's a pending confirmation, set it to false (cancel)
            _currentConfirmTcs?.TrySetResult(false);
            _currentConfirmTcs = null;
            
            OnDialogClose?.Invoke();
            return Task.CompletedTask;
        }
        
        // Additional helper methods for more dialog types
        
        public Task ShowSuccessAsync(string title, string message, string okText = "OK")
        {
            var options = new DialogOptions
            {
                Title = title,
                Message = message,
                OkText = okText,
                DialogType = DialogType.Success
            };

            OnDialogShow?.Invoke(options);
            return Task.CompletedTask;
        }
        
        public Task ShowErrorAsync(string title, string message, string okText = "OK")
        {
            var options = new DialogOptions
            {
                Title = title,
                Message = message,
                OkText = okText,
                DialogType = DialogType.Error
            };

            OnDialogShow?.Invoke(options);
            return Task.CompletedTask;
        }
    }

    public class DialogOptions
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public RenderFragment? ContentTemplate { get; set; }
        public DialogType DialogType { get; set; } = DialogType.Default;
        public string OkText { get; set; } = "OK";
        public string ConfirmText { get; set; } = "Confirm";
        public string CancelText { get; set; } = "Cancel";
        public Action<bool>? OnConfirmCallback { get; set; }
        public bool ShowCloseButton { get; set; } = true;
        public bool CloseOnOverlayClick { get; set; } = true;
    }
}
