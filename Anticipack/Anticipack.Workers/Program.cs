using Anticipack.Workers.Bot;
using Anticipack.Workers.Services;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// Register Telegram bot client
var botToken = builder.Configuration["Telegram:BotToken"]
    ?? throw new InvalidOperationException("Telegram:BotToken configuration is required.");

builder.Services.AddSingleton(new TelegramBotClient(botToken));

// Register HTTP client for API calls
builder.Services.AddHttpClient<IAnticipackApiClient, AnticipackApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7001";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register services
builder.Services.AddSingleton<IUserSessionManager, UserSessionManager>();
builder.Services.AddSingleton<IBotUpdateHandler, BotUpdateHandler>();

// Register hosted service
builder.Services.AddHostedService<TelegramBotService>();

var host = builder.Build();
host.Run();
