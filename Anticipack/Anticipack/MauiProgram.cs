using Anticipack.Components.Shared.DialogComponent;
using Anticipack.Components.Shared.ToastComponent;
using Anticipack.Components.Shared.NavigationHeaderComponent;
using Anticipack.Storage;
using Anticipack.Storage.Repositories;
using Anticipack.Services;
using Anticipack.Services.AI;
using Anticipack.Services.Categories;
using Anticipack.Services.Notifications;
using Anticipack.Services.Packing;
using Anticipack.Services.Payment;
using Anticipack.Services.Statistics;
using Anticipack.Services.Suggestions;
using Anticipack.Services.Sync;
using Microsoft.Extensions.DependencyInjection;
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
            builder.Services.AddSingleton<IAutoArchiveService, AutoArchiveService>();
            builder.Services.AddSingleton<IPackingReminderService, PackingReminderService>();
            builder.Services.AddScoped<IPackingActivityService, PackingActivityService>();
            builder.Services.AddSingleton<ICategoryIconProvider, CategoryIconProvider>();
            builder.Services.AddScoped<IPackingStatisticsService, PackingStatisticsService>();
            builder.Services.AddScoped<IItemSuggestionService, ItemSuggestionService>();

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
            builder.Services.AddSingleton<INotificationManagerService, Anticipack.Platforms.Android.Notifications.NotificationManagerService>();
#elif IOS
            builder.Services.AddSingleton<Anticipack.Services.IKeyboardService, Anticipack.Platforms.iOS.KeyboardService>();
            builder.Services.AddSingleton<INotificationManagerService, Anticipack.Platforms.iOS.Notifications.NotificationManagerService>();
#elif MACCATALYST
            builder.Services.AddSingleton<INotificationManagerService, Anticipack.Platforms.MacCatalyst.Notifications.NotificationManagerService>();
#elif WINDOWS
            builder.Services.AddSingleton<Anticipack.Services.IKeyboardService, Anticipack.Platforms.Windows.KeyboardService>();
            builder.Services.AddSingleton<INotificationManagerService, Anticipack.Platforms.Windows.Notifications.NotificationManagerService>();
#endif

            builder.Services.AddScoped<IDialogService, DialogService>();
            builder.Services.AddScoped<IToastService, ToastService>();
            builder.Services.AddSingleton<INavigationHeaderService, NavigationHeaderService>();
            builder.Services.AddSingleton<IMicrophonePermissionService, MicrophonePermissionService>();
            builder.Services.AddScoped<MicrophonePermissionBridge>();
            builder.Services.AddScoped<IPackingHistoryService, PackingHistoryService>();

            // Payment services (ISP: Segregated interfaces, DIP: Depend on abstractions)
            builder.Services.AddSingleton<PayPalConfiguration>(_ => new PayPalConfiguration
            {
                // TODO: Replace with your PayPal credentials
                ClientId = "",
                BusinessEmail = "donate@anticipack.com",
                PayPalMeUsername = ""
            });
            builder.Services.AddSingleton<IStoreService, StoreService>();
            builder.Services.AddSingleton<IPayPalService, PayPalService>();
            builder.Services.AddSingleton<IPaymentService, PaymentService>();

            // AI suggestion service (DIP: Depend on abstraction)
            builder.Services.AddSingleton<AiServiceConfiguration>(_ => new AiServiceConfiguration
            {
                ApiKey = "", // TODO: Set your Gemini API key
                Model = "gemini-2.0-flash"
            });
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<IAiSuggestionService, GeminiSuggestionService>();

            var app = builder.Build();

            return app;
        }
    }
}