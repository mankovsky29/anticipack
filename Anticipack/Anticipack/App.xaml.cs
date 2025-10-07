using Microsoft.Maui;
using Microsoft.Maui.Controls;
#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.Graphics;
#endif

namespace Anticipack
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "Anticipack" };

#if WINDOWS
            // Resize the native WinUI window once it's created.
            // This runs only on Windows and uses WinAppSDK windowing APIs.
            window.Created += (_, _) =>
            {
                try
                {
                    if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                    {
                        var hwnd = WindowNative.GetWindowHandle(nativeWindow);
                        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                        var appWindow = AppWindow.GetFromWindowId(windowId);

                        // Set the desired window size (pixels).
                        // Change these values to match the device viewport you want to emulate.
                        // Example values (logical pixels) — adjust as needed:
                        var desiredWidth = 600;   // e.g. narrow phone/large mobile viewport width
                        var desiredHeight = 1100;  // e.g. tall mobile viewport height

                        appWindow.Resize(new SizeInt32(desiredWidth, desiredHeight));

                        //// Center the window on the current display
                        //var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                        //var centerX = displayArea.WorkArea.X + (displayArea.WorkArea.Width - desiredWidth) / 2;
                        //var centerY = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - desiredHeight) / 2;
                        //appWindow.Move(new PointInt32((int)centerX, (int)centerY));
                    }
                }
                catch
                {
                    // Ignore errors when windowing APIs are not available.
                }
            };
#endif

            return window;
        }
    }
}
