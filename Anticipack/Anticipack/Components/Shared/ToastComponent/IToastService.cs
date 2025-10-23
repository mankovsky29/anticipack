namespace Anticipack.Components.Shared.ToastComponent
{
    public interface IToastService
    {
        /// <summary>
        /// An event that is invoked when showing a toast
        /// </summary>
        event Action<string, ToastLevel, int> OnShow;
        
        /// <summary>
        /// An event that is invoked when hiding a toast
        /// </summary>
        event Action OnHide;
        
        /// <summary>
        /// Shows a standard toast message
        /// </summary>
        void Show(string message, ToastLevel level = ToastLevel.Info, int durationMs = 3000);
        
        /// <summary>
        /// Shows a success toast message
        /// </summary>
        void ShowSuccess(string message, int durationMs = 3000);
        
        /// <summary>
        /// Shows an error toast message
        /// </summary>
        void ShowError(string message, int durationMs = 5000);
        
        /// <summary>
        /// Shows an info toast message
        /// </summary>
        void ShowInfo(string message, int durationMs = 3000);
        
        /// <summary>
        /// Shows a warning toast message
        /// </summary>
        void ShowWarning(string message, int durationMs = 4000);
    }

    public enum ToastLevel
    {
        Info,
        Success,
        Warning,
        Error
    }
}
