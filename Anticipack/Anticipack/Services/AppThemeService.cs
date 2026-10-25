namespace Anticipack.Services
{
    public class AppThemeService : IAppThemeService
    {
        private string _theme = "auto";
        private bool _subscribed;

        public void Apply(string theme)
        {
            _theme = theme;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var app = Application.Current;
                if (app is null)
                    return;

#if IOS || MACCATALYST
                // The status bar style follows the window's interface style, which tracks UserAppTheme.
                // Not used on Android: it switches AppCompat night mode, which recreates the activity.
                app.UserAppTheme = theme switch
                {
                    "light" => AppTheme.Light,
                    "dark" => AppTheme.Dark,
                    _ => AppTheme.Unspecified
                };
#endif

                if (!_subscribed)
                {
                    // "auto" must keep tracking system light/dark switches
                    app.RequestedThemeChanged += (_, _) => UpdateSystemBars(app);
                    _subscribed = true;
                }

                UpdateSystemBars(app);
            });
        }

        private void UpdateSystemBars(Application app)
        {
#if ANDROID
            var window = Platform.CurrentActivity?.Window;
            if (window is null)
                return;

            var isLight = _theme switch
            {
                "light" => true,
                "dark" => false,
                _ => app.RequestedTheme != AppTheme.Dark
            };

            // Edge-to-edge: bars are transparent over the WebView, so only the icon contrast needs setting
            var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
            controller.AppearanceLightStatusBars = isLight;
            controller.AppearanceLightNavigationBars = isLight;
#endif
        }
    }
}
