using Anticipack.Storage.Repositories;

namespace Anticipack.Services.Suggestions;

/// <summary>
/// Provides autocomplete suggestions for packing items based on historical usage
/// (SRP: Suggestion logic separated from UI components)
/// </summary>
public interface IItemSuggestionService
{
    /// <summary>
    /// Loads all item names and their category/frequency data from the repository.
    /// Must be called before requesting suggestions.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Returns up to <paramref name="maxResults"/> autocomplete suggestions based on the
    /// current search token, selected category, and items already present or being added.
    /// When <paramref name="searchToken"/> is empty, returns the most popular items in the category.
    /// </summary>
    List<string> GetSuggestions(
        string searchToken,
        string selectedCategory,
        IReadOnlySet<string> existingItemNames,
        IReadOnlyCollection<string> parsedItems,
        int maxResults = 3);
}
