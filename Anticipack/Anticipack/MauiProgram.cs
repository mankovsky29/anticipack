using Anticipack.Components.Shared.DialogComponent;
using Anticipack.Components.Shared.ToastComponent;
using Anticipack.Components.Shared.NavigationHeaderComponent;
using Anticipack.Storage;
using Anticipack.Storage.Repositories;
using Anticipack.Services;
using Anticipack.Services.Categories;
using Anticipack.Services.Packing;
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

            // Database connection factory (DIP: Abstraction for database access)
            builder.Services.AddSingleton<IDatabaseConnectionFactory>(_ => 
                new SqliteDatabaseConnectionFactory(dbPath));

            // Register focused repositories (ISP: Interface Segregation)
            builder.Services.AddSingleton<IPackingItemRepository, PackingItemRepository>();
            builder.Services.AddSingleton<IPackingHistoryRepository, PackingHistoryRepository>();
            builder.Services.AddSingleton<IPackingActivityRepository, PackingActivityRepository>();

            // Legacy repository for backward compatibility
#pragma warning disable CS0618 // Intentionally registering legacy interface
            builder.Services.AddSingleton<IPackingRepository>(s => 
            {
                var repo = new PackingRepository(dbPath);
                return repo; 
            });
#pragma warning restore CS0618

            // Business services (SRP: Separated business logic)
            builder.Services.AddScoped<IPackingActivityService, PackingActivityService>();
            builder.Services.AddSingleton<ICategoryIconProvider, CategoryIconProvider>();

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
            builder.Services.AddSingleton<INavigationHeaderService, NavigationHeaderService>();
            builder.Services.AddSingleton<IMicrophonePermissionService, MicrophonePermissionService>();
            builder.Services.AddScoped<MicrophonePermissionBridge>();
            builder.Services.AddScoped<IPackingHistoryService, PackingHistoryService>();

            var app = builder.Build();

            return app;
        }
    }
}