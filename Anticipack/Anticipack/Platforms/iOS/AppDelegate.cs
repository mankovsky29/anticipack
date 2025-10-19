using Anticipack.Services;
using Foundation;
using UIKit;

namespace Anticipack
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        private readonly IKeyboardService _keyboardService;

        public AppDelegate(IKeyboardService keyboardService)
        {
            _keyboardService = keyboardService;
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            var result = base.FinishedLaunching(application, launchOptions);

            // Initialize keyboard service with the root window
            _keyboardService.Initialize(GetKeyWindow());

            return result;
        }

        private UIWindow? GetKeyWindow()
        {
            // Get the key window for iOS
            return UIApplication.SharedApplication.ConnectedScenes
                .OfType<UIWindowScene>()
                .SelectMany(scene => scene.Windows)
                .FirstOrDefault(window => window.IsKeyWindow);
        }
    }
}
