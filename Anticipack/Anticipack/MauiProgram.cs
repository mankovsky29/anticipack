using Anticipack.Storage;
using Microsoft.Extensions.Logging;

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

            var app = builder.Build();

            return app;
        }
    }
}