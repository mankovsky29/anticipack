using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Anticipack.Workers.Bot;

public class TelegramBotService : BackgroundService
{
    private readonly TelegramBotClient _bot;
    private readonly IBotUpdateHandler _handler;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        TelegramBotClient bot,
        IBotUpdateHandler handler,
        ILogger<TelegramBotService> logger)
    {
        _bot = bot;
        _handler = handler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram bot service starting...");

        try
        {
            var me = await _bot.GetMe(stoppingToken);
            _logger.LogInformation("Bot started: @{Username} (ID: {Id})", me.Username, me.Id);

            // Set bot commands for the menu
            await _bot.SetMyCommands(
            [
                new BotCommand { Command = "start", Description = "Welcome & connect" },
                new BotCommand { Command = "help", Description = "Show all commands" },
                new BotCommand { Command = "new", Description = "Create a new packing list" },
                new BotCommand { Command = "lists", Description = "View your packing lists" },
                new BotCommand { Command = "show", Description = "Open a list by number" },
                new BotCommand { Command = "add", Description = "Add item(s) to current list" },
                new BotCommand { Command = "pack", Description = "Toggle item packed status" },
                new BotCommand { Command = "remove", Description = "Remove an item" },
                new BotCommand { Command = "deletelist", Description = "Delete current list" },
                new BotCommand { Command = "current", Description = "Show selected list" },
            ], cancellationToken: stoppingToken);

            int offset = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var updates = await _bot.GetUpdates(offset, timeout: 30, cancellationToken: stoppingToken);

                    foreach (var update in updates)
                    {
                        offset = update.Id + 1;
                        _ = ProcessUpdateAsync(update, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling for updates");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Telegram bot service failed to start");
            throw;
        }

        _logger.LogInformation("Telegram bot service stopped.");
    }

    private async Task ProcessUpdateAsync(Update update, CancellationToken ct)
    {
        try
        {
            string? response = null;
            long chatId = 0;

            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                chatId = update.Message.Chat.Id;
                response = await _handler.HandleMessageAsync(update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                chatId = update.CallbackQuery.Message?.Chat.Id ?? 0;
                var userId = update.CallbackQuery.From.Id;
                var data = update.CallbackQuery.Data ?? "";
                response = await _handler.HandleCallbackAsync(
                    userId, data,
                    update.CallbackQuery.From.FirstName,
                    update.CallbackQuery.From.LastName,
                    update.CallbackQuery.From.Username);

                await _bot.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);
            }

            if (!string.IsNullOrEmpty(response) && chatId != 0)
            {
                await _bot.SendMessage(
                    chatId,
                    response,
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing update {UpdateId}", update.Id);
        }
    }
}
