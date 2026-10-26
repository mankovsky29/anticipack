using Anticipack.Services;
using Microsoft.JSInterop;

namespace Anticipack
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        // Only reached when the web view has no history left; it handles in-app back itself
        protected override bool OnBackButtonPressed()
        {
            var backNavigation = Handler?.MauiContext?.Services.GetService<IBackNavigationService>();
            return backNavigation?.TryHandleBack() ?? base.OnBackButtonPressed();
        }
    }
}
