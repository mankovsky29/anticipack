using UIKit;
using Foundation;
using Anticipack.Services;

[assembly: Dependency(typeof(Anticipack.Platforms.iOS.KeyboardService))]
namespace Anticipack.Platforms.iOS
{
    public class KeyboardService : IKeyboardService
    {
        public event Action<bool, double>? KeyboardVisibilityChanged;

        private NSObject? _willShowObserver;
        private NSObject? _willHideObserver;

        public void Initialize(object? platformSpecific)
        {
            _willShowObserver = UIKeyboard.Notifications.ObserveWillShow((sender, args) =>
            {
                var keyboardFrame = args.FrameEnd;
                // Convert pixel height to DIPs
                double density = DeviceDisplay.MainDisplayInfo.Density;
                double heightDp = keyboardFrame.Height / density;
                KeyboardVisibilityChanged?.Invoke(true, heightDp);
            });

            _willHideObserver = UIKeyboard.Notifications.ObserveWillHide((sender, args) =>
            {
                KeyboardVisibilityChanged?.Invoke(false, 0);
            });
        }

        ~KeyboardService()
        {
            DisposeObservers();
        }

        private void DisposeObservers()
        {
            _willShowObserver?.Dispose();
            _willHideObserver?.Dispose();
            _willShowObserver = null;
            _willHideObserver = null;
        }
    }
}
