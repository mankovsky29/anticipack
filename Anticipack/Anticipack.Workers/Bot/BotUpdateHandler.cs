using Anticipack.Workers.Models;
using Anticipack.Workers.Services;
using Telegram.Bot.Types;

namespace Anticipack.Workers.Bot;

public interface IBotUpdateHandler
{
    Task<string> HandleMessageAsync(Message message);
    Task<string> HandleCallbackAsync(long userId, string data, string? firstName, string? lastName, string? username);
}

public class BotUpdateHandler : IBotUpdateHandler
{
    private readonly IAnticipackApiClient _apiClient;
    private readonly IUserSessionManager _sessionManager;
    private readonly ILogger<BotUpdateHandler> _logger;

    public BotUpdateHandler(
        IAnticipackApiClient apiClient,
        IUserSessionManager sessionManager,
        ILogger<BotUpdateHandler> logger)
    {
        _apiClient = apiClient;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public async Task<string> HandleMessageAsync(Message message)
    {
        var userId = message.From?.Id ?? 0;
        if (userId == 0) return "";

        var text = message.Text?.Trim() ?? "";
        var firstName = message.From?.FirstName;
        var lastName = message.From?.LastName;
        var username = message.From?.Username;

        if (text.StartsWith('/'))
        {
            return await HandleCommandAsync(userId, text, firstName, lastName, username);
        }

        // Free text — try to add as item to current list
        return await HandleFreeTextAsync(userId, text, firstName, lastName, username);
    }

    public async Task<string> HandleCallbackAsync(long userId, string data, string? firstName, string? lastName, string? username)
    {
        var parts = data.Split(':');
        if (parts.Length < 2) return "❌ Invalid action.";

        var action = parts[0];
        var id = parts[1];

        var token = await _sessionManager.GetOrCreateTokenAsync(userId, firstName, lastName, username);
        if (token == null) return "❌ Authentication failed. Try /start again.";

        return action switch
        {
            "select" => await SelectActivityAsync(userId, token, id),
            "pack" => await ToggleItemAsync(token, id),
            "delitem" => await DeleteItemFromCallbackAsync(token, id),
            "dellist" => await DeleteListAsync(userId, token, id),
            _ => "❌ Unknown action."
        };
    }

    private async Task<string> HandleCommandAsync(long userId, string text, string? firstName, string? lastName, string? username)
    {
        var parts = text.Split(' ', 2);
        var command = parts[0].ToLowerInvariant().Split('@')[0]; // handle /command@botname
        var argument = parts.Length > 1 ? parts[1].Trim() : "";

        return command switch
        {
            "/start" => await HandleStartAsync(userId, firstName, lastName, username),
            "/help" => GetHelpText(),
            "/new" or "/create" => await HandleNewListAsync(userId, argument, firstName, lastName, username),
            "/lists" or "/mylists" => await HandleListsAsync(userId, firstName, lastName, username),
            "/show" or "/open" => await HandleShowAsync(userId, argument, firstName, lastName, username),
            "/add" => await HandleAddItemAsync(userId, argument, firstName, lastName, username),
            "/pack" => await HandlePackItemAsync(userId, argument, firstName, lastName, username),
            "/remove" or "/rm" => await HandleRemoveItemAsync(userId, argument, firstName, lastName, username),
            "/deletelist" => await HandleDeleteListAsync(userId, firstName, lastName, username),
            "/current" => HandleCurrentList(userId),
            _ => "❓ Unknown command. Type /help to see available commands."
        };
    }

    private async Task<string> HandleStartAsync(long userId, string? firstName, string? lastName, string? username)
    {
        var token = await _sessionManager.GetOrCreateTokenAsync(userId, firstName, lastName, username);
        if (token == null)
        {
            return "❌ Could not connect to Anticipack. Please try again later.";
        }

        var name = firstName ?? "there";
        return $"""
            👋 Welcome to *Anticipack Bot*, {EscapeMarkdown(name)}!

            I help you manage your packing lists right from Telegram\.

            📋 *Quick Start:*
            /new `Trip Name` — Create a new packing list
            /lists — View your lists
            /add `item` — Add item to current list

            Type /help for all commands\.
            """;
    }

    private static string GetHelpText()
    {
        return """
            📦 *Anticipack Bot Commands*

            *Lists:*
            /new `name` — Create a new packing list
            /lists — Show all your lists
            /show `number` — Open a list by its number
            /deletelist — Delete the current list
            /current — Show current selected list

            *Items:*
            /add `item` — Add item to current list
            /add `item1, item2, item3` — Add multiple items
            /pack `number` — Toggle packed status
            /remove `number` — Remove an item

            💡 *Tip:* Just type item names directly and they'll be added to your current list\!
            """;
    }

    private async Task<string> HandleNewListAsync(long userId, string name, string? firstName, string? lastName, string? username)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "📝 Please provide a name: `/new Trip to Paris`";
        }

        var token = await _sessionManager.GetOrCreateTokenAsync(userId, firstName, lastName, username);
        if (token == null) return AuthError();

        var activity = await _apiClient.CreateActivityAsync(token, name);
        if (activity == null) return "❌ Failed to create list. Please try again.";

        _sessionManager.SetCurrentActivity(userId, activity.Id);

        return $"""
            ✅ List "*{EscapeMarkdown(activity.Name)}*" created\!

            Now add items with:
            /add `Passport`
            /add `Clothes, Charger, Toothbrush`

            Or just type item names directly\.
            """;
    }

    private async Task<string> HandleListsAsync(long userId, string? firstName, string? lastName, string? username)
    {
        var token = await _sessionManager.GetOrCreateTokenAsync(userId, firstName, lastName, username);
        if (token == null) return AuthError();

        var activities = await _apiClient.GetActivitiesAsync(token);
        if (activities.Count == 0)
        {
            return "📭 You don't have any packing lists yet\\.\n\nCreate one with /new `Trip Name`";
        }

        var currentId = _sessionManager.GetCurrentActivityId(userId);
        var lines = new List<string> { "📋 *Your Packing Lists:*\n" };

        for (int i = 0; i < activities.Count; i++)
        {
            var a = activities[i];
            var packed = a.Items.Count(x => x.IsPacked);
            var total = a.Items.Count;
            var indicator = currentId == a.Id ? "👉 " : "   ";
            var status = a.IsFinished ? "✅" : a.IsArchived ? "📦" : "📋";
            var progress = total > 0 ? $" \\({packed}/{total}\\)" : "";

            lines.Add($"{indicator}{status} *{i + 1}\\.* {EscapeMarkdown(a.Name)}{progress}");
        }

        lines.Add("\nUse /show `number` to open a list\\.");

        return string.Join("\n", lines);
    }

    private async Task<string> HandleShowAsync(long userId, string argument, string? firstName, string? lastName, string? username)
    {
        var token = await _sessionManager.GetOrCreateTokenAsync(userId, firstName, lastName, username);
        if (token == null) return AuthError();

        var activities = await _apiClient.GetActivitiesAsync(token);
        if (activities.Count == 0)
        {
            return "📭 No lists found\\. Create one with /new `Trip Name`";
        }

        int index;
        if (string.IsNullOrWhiteSpace(argument))
        {
            // Show current list
            var currentId = _sessionManager.GetCurrentActivityId(userId);
            if (currentId == null)
                return "📝 Please specify a list number: `/show 1`";

            index = activities.FindIndex(a => a.Id == currentId);
            if (index < 0)
                return "📝 Current list not found\\. Use /lists to see your lists\\.";
        }
        else if (!int.TryParse(argument, out index) || index < 1 || index > activities.Count)
        {
            return $"❌ Invalid number\\. You have {activities.Count} list\\(s\\)\\. Use /show `1`\\-`{activities.Count}`";
        }
        else
        {
            index--;
        }

        var activity = activities[index];
        _sessionManager.SetCurrentActivity(userId, activity.Id);

        return FormatActivityDetail(activity, index + 1);
    }

    private async Task<string> HandleAddItemAsync(long userId, string itemText, string? firstName, string? lastName, string? username)
    {
        if (string.IsNullOrWhiteSpace(itemText))
        {
            return "📝 Please provide item names: `/add Passport` or `/add Shirt, Pants, Belt`";
        }

        var token = await _sessionManager.GetOrCreateTokenAsync(userId, firstName, lastName, username);
        if (token == null) return AuthError();

        var activityId = _sessionManager.GetCurrentActivityId(userId);
        if (activityId == null)
        {
            return "📝 No list selected\\. Use /lists and /show to select one, or /new to create one\\.";
        }

        var itemNames = itemText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var added = new List<string>();

        foreach (var name in itemNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var item = await _apiClient.AddItemAsync(token, activityId, name);
            if (item != null) added.Add(item.Name);
        }

        if (added.Count == 0)
            return "❌ Failed to add items\\. Please try again\\.";

        if (added.Count == 1)
            return $"✅ Added: *{EscapeMarkdown(added[0])}*";

        var list = string.Join("\n", added.Select(n => $"  • {EscapeMarkdown(n)}"));
        return $"✅ Added {added.Count} items:\n{list}";
    }

    private async Task<string> HandlePackItemAsync(long userId, string argument, string? firstName, string? lastName, string? username)
    {
        if (!int.TryParse(argument, out var itemNumber) || itemNumber < 1)
        {
            return "📝 Usage: `/pack 1` \\(item number from /show\\)";
        }

        var token = await _sessionManager.GetOrCreateTokenAsync(userId, firstName, lastName, username);
        if (token == null) return AuthError();

        var activityId = _sessionManager.GetCurrentActivityId(userId);
        if (activityId == null)
            return "📝 No list selected\\. Use /show to select one\\.";

        var activity = await _apiClient.GetActivityAsync(token, activityId);
        if (activity == null) return "❌ List not found\\.";

        if (itemNumber > activity.Items.Count)
            return $"❌ Item number out of range\\. List has {activity.Items.Count} item\\(s\\)\\.";

        var item = activity.Items[itemNumber - 1];
        var toggled = await _apiClient.ToggleItemAsync(token, activityId, item.Id);
        if (toggled == null) return "❌ Failed to update item\\.";

        var emoji = toggled.IsPacked ? "✅" : "⬜";
        var action = toggled.IsPacked ? "packed" : "unpacked";
        return $"{emoji} *{EscapeMarkdown(toggled.Name)}* {action}\\!";
    }

    private async Task<string> HandleRemoveItemAsync(long userId, string argument, string? firstName, string? lastName, string? username)
    {
        if (!int.TryParse(argument, out var itemNumber) || itemNumber < 1)
        {
            return "📝 Usage: `/remove 1` \\(item number from /show\\)";
        }

        var token = await _sessionManager.GetOrCreateTokenAsync(userId, firstName, lastName, username);
        if (token == null) return AuthError();

        var activityId = _sessionManager.GetCurrentActivityId(userId);
        if (activityId == null)
            return "📝 No list selected\\. Use /show to select one\\.";

        var activity = await _apiClient.GetActivityAsync(token, activityId);
        if (activity == null) return "❌ List not found\\.";

        if (itemNumber > activity.Items.Count)
            return $"❌ Item number out of range\\. List has {activity.Items.Count} item\\(s\\)\\.";

        var item = activity.Items[itemNumber - 1];
        var deleted = await _apiClient.DeleteItemAsync(token, activityId, item.Id);

        return deleted
            ? $"🗑️ Removed: *{EscapeMarkdown(item.Name)}*"
            : "❌ Failed to remove item\\.";
    }

    private async Task<string> HandleDeleteListAsync(long userId, string? firstName, string? lastName, string? username)
    {
        var token = await _sessionManager.GetOrCreateTokenAsync(userId, firstName, lastName, username);
        if (token == null) return AuthError();

        var activityId = _sessionManager.GetCurrentActivityId(userId);
        if (activityId == null)
            return "📝 No list selected\\. Use /show to select one first\\.";

        var activity = await _apiClient.GetActivityAsync(token, activityId);
        if (activity == null) return "❌ List not found\\.";

        var deleted = await _apiClient.DeleteActivityAsync(token, activityId);
        if (!deleted) return "❌ Failed to delete list\\.";

        _sessionManager.ClearCurrentActivity(userId);
        return $"🗑️ List \"*{EscapeMarkdown(activity.Name)}*\" deleted\\.";
    }

    private string HandleCurrentList(long userId)
    {
        var activityId = _sessionManager.GetCurrentActivityId(userId);
        if (activityId == null)
            return "📝 No list selected\\. Use /lists and /show to select one\\.";

        return $"📋 Current list ID: `{activityId}`\nUse /show to view its items\\.";
    }

    private async Task<string> HandleFreeTextAsync(long userId, string text, string? firstName, string? lastName, string? username)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        var activityId = _sessionManager.GetCurrentActivityId(userId);
        if (activityId == null)
        {
            return "📝 No list selected\\. Use /lists and /show to select one, or /new to create one\\.\n\nType /help for all commands\\.";
        }

        // Treat the text as item(s) to add
        var token = await _sessionManager.GetOrCreateTokenAsync(userId, firstName, lastName, username);
        if (token == null) return AuthError();

        var itemNames = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var added = new List<string>();

        foreach (var name in itemNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var item = await _apiClient.AddItemAsync(token, activityId, name);
            if (item != null) added.Add(item.Name);
        }

        if (added.Count == 0) return "❌ Failed to add items\\.";

        if (added.Count == 1)
            return $"✅ Added: *{EscapeMarkdown(added[0])}*";

        var list = string.Join("\n", added.Select(n => $"  • {EscapeMarkdown(n)}"));
        return $"✅ Added {added.Count} items:\n{list}";
    }

    private async Task<string> SelectActivityAsync(long userId, string token, string activityId)
    {
        var activity = await _apiClient.GetActivityAsync(token, activityId);
        if (activity == null) return "❌ List not found\\.";

        _sessionManager.SetCurrentActivity(userId, activityId);
        return FormatActivityDetail(activity, null);
    }

    private async Task<string> ToggleItemAsync(string token, string combinedId)
    {
        var parts = combinedId.Split('|');
        if (parts.Length != 2) return "❌ Invalid item reference\\.";

        var activityId = parts[0];
        var itemId = parts[1];

        var toggled = await _apiClient.ToggleItemAsync(token, activityId, itemId);
        if (toggled == null) return "❌ Failed to update item\\.";

        var emoji = toggled.IsPacked ? "✅" : "⬜";
        var action = toggled.IsPacked ? "packed" : "unpacked";
        return $"{emoji} *{EscapeMarkdown(toggled.Name)}* {action}\\!";
    }

    private async Task<string> DeleteItemFromCallbackAsync(string token, string combinedId)
    {
        var parts = combinedId.Split('|');
        if (parts.Length != 2) return "❌ Invalid item reference\\.";

        var activityId = parts[0];
        var itemId = parts[1];

        var deleted = await _apiClient.DeleteItemAsync(token, activityId, itemId);
        return deleted ? "🗑️ Item removed\\." : "❌ Failed to remove item\\.";
    }

    private async Task<string> DeleteListAsync(long userId, string token, string activityId)
    {
        var deleted = await _apiClient.DeleteActivityAsync(token, activityId);
        if (!deleted) return "❌ Failed to delete list\\.";

        _sessionManager.ClearCurrentActivity(userId);
        return "🗑️ List deleted\\.";
    }

    private static string FormatActivityDetail(ActivityDto activity, int? number)
    {
        var header = number.HasValue
            ? $"📋 *{number}\\. {EscapeMarkdown(activity.Name)}*"
            : $"📋 *{EscapeMarkdown(activity.Name)}*";

        var lines = new List<string> { header };

        if (activity.Items.Count == 0)
        {
            lines.Add("\n_No items yet\\._");
            lines.Add("\nAdd items with /add `item name`");
        }
        else
        {
            lines.Add("");
            for (int i = 0; i < activity.Items.Count; i++)
            {
                var item = activity.Items[i];
                var emoji = item.IsPacked ? "✅" : "⬜";
                var categoryTag = !string.IsNullOrEmpty(item.Category) ? $" \\[{EscapeMarkdown(item.Category)}\\]" : "";
                lines.Add($"{emoji} {i + 1}\\. {EscapeMarkdown(item.Name)}{categoryTag}");
            }

            var packed = activity.Items.Count(x => x.IsPacked);
            lines.Add($"\n📊 Progress: {packed}/{activity.Items.Count}");
        }

        lines.Add("\n_Use /pack, /add, /remove to manage items_");

        return string.Join("\n", lines);
    }

    private static string AuthError() => "❌ Authentication failed\\. Try /start again\\.";

    private static string EscapeMarkdown(string text)
    {
        // Escape MarkdownV2 special characters
        var specialChars = new[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        foreach (var c in specialChars)
        {
            text = text.Replace(c.ToString(), $"\\{c}");
        }
        return text;
    }
}
