namespace Anticipack.Services.AI;

public interface IAiSuggestionService
{
    Task<List<AiSuggestedItem>> SuggestItemsAsync(
        string prompt,
        string? activityName,
        string? category,
        IReadOnlyList<string> existingItems,
        CancellationToken cancellationToken = default);
}
