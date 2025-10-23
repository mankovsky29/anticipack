using System;
using System.Timers;

namespace Anticipack.Components.Shared.ToastComponent
{
    public class ToastService : IToastService, IDisposable
    {
        public event Action<string, ToastLevel, int>? OnShow;
        public event Action? OnHide;
        private System.Timers.Timer? _countdown;

        public void Show(string message, ToastLevel level = ToastLevel.Info, int durationMs = 3000)
        {
            DisposeTimer();

            _countdown = new System.Timers.Timer(durationMs);
            _countdown.Elapsed += HideToast;
            _countdown.AutoReset = false;
            _countdown.Start();

            OnShow?.Invoke(message, level, durationMs);
        }

        public void ShowSuccess(string message, int durationMs = 2000)
            => Show(message, ToastLevel.Success, durationMs);

        public void ShowError(string message, int durationMs = 3000)
            => Show(message, ToastLevel.Error, durationMs);

        public void ShowInfo(string message, int durationMs = 2000)
            => Show(message, ToastLevel.Info, durationMs);

        public void ShowWarning(string message, int durationMs = 3000)
            => Show(message, ToastLevel.Warning, durationMs);

        private void HideToast(object? source, ElapsedEventArgs args)
        {
            OnHide?.Invoke();
            DisposeTimer();
        }

        private void DisposeTimer()
        {
            if (_countdown != null)
            {
                _countdown.Stop();
                _countdown.Elapsed -= HideToast;
                _countdown.Dispose();
                _countdown = null;
            }
        }

        public void Dispose()
        {
            DisposeTimer();
        }
    }
}
