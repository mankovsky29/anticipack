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

            var app = builder.Build();

            return app;
        }
    }
}