using UIKit;
using Foundation;
using Anticipack.Services;

[assembly: Dependency(typeof(Anticipack.Platforms.iOS.KeyboardService))]
namespace Anticipack.Platforms.iOS
{
    public class KeyboardService : IKeyboardService
    {
        public event Action<bool>? KeyboardVisibilityChanged;

        private NSObject? _willShowObserver;
        private NSObject? _willHideObserver;

        public void Initialize(object? platformSpecific)
        {
            // Subscribe to keyboard notifications
            _willShowObserver = UIKeyboard.Notifications.ObserveWillShow((sender, args) =>
            {
                KeyboardVisibilityChanged?.Invoke(true);
            });

            _willHideObserver = UIKeyboard.Notifications.ObserveWillHide((sender, args) =>
            {
                KeyboardVisibilityChanged?.Invoke(false);
            });
        }

        ~KeyboardService()
        {
            DisposeObservers();
        }

        private void DisposeObservers()
        {
            _willShowObserver?.Dispose();
            _willShowObserver = null;
            _willHideObserver?.Dispose();
            _willHideObserver = null;
        }
    }
}
