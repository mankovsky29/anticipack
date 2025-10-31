using Anticipack.Storage;

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
        private readonly IPackingRepository _packingRepository;

        public App(IPackingRepository packingRepository)
        {
            InitializeComponent();
            _packingRepository = packingRepository;
            
            // Add global exception handler
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("❌ UNHANDLED DOMAIN EXCEPTION");
                System.Diagnostics.Debug.WriteLine($"Exception: {exception?.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"Message: {exception?.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace:\n{exception?.StackTrace}");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");
            };

            // Also catch task exceptions
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("❌ UNOBSERVED TASK EXCEPTION");
                System.Diagnostics.Debug.WriteLine($"Exception: {args.Exception.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"Message: {args.Exception.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace:\n{args.Exception.StackTrace}");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");
                args.SetObserved(); // Prevent app crash
            };
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Window creation error: {ex.Message}");
                }
            };
#endif

            return window;
        }

        protected override async void OnStart()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                }

                if (status != PermissionStatus.Granted)
                {
                    throw new Exception("Storage permission is required to use this feature.");
                }

                await _packingRepository.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ OnStart Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
            }
        }
    }
}
