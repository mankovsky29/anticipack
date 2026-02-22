using System.Net.Http.Json;
using System.Text.Json;

namespace Anticipack.Services.AI;

public class GeminiSuggestionService : IAiSuggestionService
{
    private readonly HttpClient _httpClient;
    private readonly AiServiceConfiguration _configuration;

    private static readonly HashSet<string> ValidCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Clothing", "Shoes", "Toiletries", "Electronics", "Documents",
        "Health", "Accessories", "Outdoor", "Food", "Entertainment", "Miscellaneous"
    };

    public GeminiSuggestionService(HttpClient httpClient, AiServiceConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<AiSuggestedItem>> SuggestItemsAsync(
        string prompt,
        string? activityName,
        string? category,
        IReadOnlyList<string> existingItems,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_configuration.ApiKey))
            throw new InvalidOperationException("AI service API key is not configured.");

        var fullPrompt = BuildPrompt(prompt, activityName, category, existingItems);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = fullPrompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 1024,
                responseMimeType = "application/json"
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_configuration.Model}:generateContent?key={_configuration.ApiKey}";

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        var text = json
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
            return [];

        var items = JsonSerializer.Deserialize<List<ItemDto>>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (items is null)
            return [];

        return items
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .Select(i => new AiSuggestedItem(
                i.Name!.Trim(),
                ValidCategories.Contains(i.Category ?? "") ? i.Category! : "Miscellaneous"))
            .ToList();
    }

    private static string BuildPrompt(
        string userPrompt,
        string? activityName,
        string? category,
        IReadOnlyList<string> existingItems)
    {
        var categoryConstraint = !string.IsNullOrWhiteSpace(category)
            ? $"Only suggest items in the \"{category}\" category."
            : "Assign each item to the most appropriate category from the list.";

        var existingItemsList = existingItems.Count > 0
            ? $"Already packed items (do NOT suggest these): {string.Join(", ", existingItems)}"
            : "No items packed yet.";

        return $$"""
            You are a packing assistant. Suggest practical packing items based on the user's request.

            Valid categories: Clothing, Shoes, Toiletries, Electronics, Documents, Health, Accessories, Outdoor, Food, Entertainment, Miscellaneous.
            {{categoryConstraint}}

            Activity name: {{activityName ?? "Unknown"}}
            User request: {{userPrompt}}
            {{existingItemsList}}

            Return a JSON array of objects with "name" and "category" fields. Suggest 5-15 relevant items.
            Example: [{"name": "Sunscreen", "category": "Toiletries"}, {"name": "Swimsuit", "category": "Clothing"}]
            """;
    }

    private record ItemDto(string? Name, string? Category);
}
