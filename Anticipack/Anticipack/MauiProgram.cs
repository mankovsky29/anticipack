using Anticipack.Components.Shared.DialogComponent;
using Anticipack.Components.Shared.ToastComponent;
using Anticipack.Storage;
using Anticipack.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using System.Reflection;

namespace Anticipack
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "packing.db3");
            builder.Services.AddMauiBlazorWebView();

            builder.Services.AddSingleton<IPackingRepository>(s => 
            {
                var repo = new PackingRepository(dbPath);
                return repo; 
            });

            // Add Localization Services
            builder.Services.AddLocalization();
            
            // Register a custom string localizer that uses AppResources
            builder.Services.AddSingleton<IStringLocalizer>(sp =>
            {
                var factory = sp.GetRequiredService<IStringLocalizerFactory>();
                return factory.Create("AppResources", "Anticipack.Resources.Localization");
            });
            
            builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif


#if ANDROID
            builder.Services.AddSingleton<Anticipack.Services.IKeyboardService, Anticipack.Platforms.Android.KeyboardService>();
#elif IOS
            builder.Services.AddSingleton<Anticipack.Services.IKeyboardService, Anticipack.Platforms.iOS.KeyboardService>();

#elif WINDOWS
            builder.Services.AddSingleton<Anticipack.Services.IKeyboardService, Anticipack.Platforms.Windows.KeyboardService>();
#endif

            builder.Services.AddScoped<IDialogService, DialogService>();
            builder.Services.AddScoped<IToastService, ToastService>();

            var app = builder.Build();

            return app;
        }
    }
}