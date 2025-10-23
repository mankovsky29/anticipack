using Anticipack.Platforms.Android;
using Anticipack.Services;
using Android.App;

[assembly: Dependency(typeof(KeyboardService))]
namespace Anticipack.Platforms.Android
{
    public class KeyboardService : IKeyboardService
    {
        public event Action<bool, double>? KeyboardVisibilityChanged;

        // optional store to allow manual initialization
        private global::Android.Views.View? _rootView;
        private int _lastVisibleHeight = 0;

        public void Initialize(object? platformSpecific)
        {
            if (platformSpecific is not Activity activity)
                return;

            _rootView = activity.Window?.DecorView?.RootView;
            if (_rootView == null)
                return;

            _rootView.ViewTreeObserver.GlobalLayout += OnGlobalLayout;
        }

        private void OnGlobalLayout(object? sender, EventArgs e)
        {
            if (_rootView == null)
                return;

            var rect = new global::Android.Graphics.Rect();
            _rootView.GetWindowVisibleDisplayFrame(rect);

            int visibleHeight = rect.Height();
            if (_lastVisibleHeight == 0)
            {
                _lastVisibleHeight = visibleHeight;
                return;
            }

            int delta = _lastVisibleHeight - visibleHeight;

            if (Math.Abs(delta) > 150)
            {
                bool isVisible = delta > 0;

                double keyboardHeightDp = 0;
                if (isVisible)
                {
                    // Convert pixel height to device-independent units
                    double density = DeviceDisplay.MainDisplayInfo.Density;
                    keyboardHeightDp = delta / density;
                }

                KeyboardVisibilityChanged?.Invoke(isVisible, keyboardHeightDp);
            }

            _lastVisibleHeight = visibleHeight;
        }
    }
}