using Anticipack.Storage.Repositories;

namespace Anticipack.Services.Suggestions;

/// <summary>
/// Default implementation that ranks suggestions by category affinity and usage frequency.
/// </summary>
public sealed class ItemSuggestionService : IItemSuggestionService
{
    private readonly IPackingItemRepository _itemRepository;

    private List<string> _allDistinctItemNames = [];
    private Dictionary<string, HashSet<string>> _itemNameCategories = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, int> _itemNameFrequency = new(StringComparer.OrdinalIgnoreCase);

    public ItemSuggestionService(IPackingItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        try
        {
            var allItems = await _itemRepository.GetAllItemsAsync();

            _allDistinctItemNames = allItems
                .Select(i => i.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();

            _itemNameCategories = allItems
                .Where(i => !string.IsNullOrWhiteSpace(i.Name) && !string.IsNullOrWhiteSpace(i.Category))
                .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => new HashSet<string>(g.Select(i => i.Category), StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

            _itemNameFrequency = allItems
                .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count(),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            _allDistinctItemNames = [];
            _itemNameCategories = new(StringComparer.OrdinalIgnoreCase);
            _itemNameFrequency = new(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <inheritdoc />
    public List<string> GetSuggestions(
        string searchToken,
        string selectedCategory,
        IReadOnlySet<string> existingItemNames,
        IReadOnlyCollection<string> parsedItems,
        int maxResults = 3)
    {
        var alreadyAdding = new HashSet<string>(parsedItems, StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(searchToken))
        {
            return _allDistinctItemNames
                .Where(name => !existingItemNames.Contains(name)
                            && !alreadyAdding.Contains(name)
                            && _itemNameCategories.TryGetValue(name, out var cats)
                            && cats.Contains(selectedCategory, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(name => _itemNameFrequency.GetValueOrDefault(name, 0))
                .Take(maxResults)
                .ToList();
        }

        bool IsSameCategory(string name) =>
            _itemNameCategories.TryGetValue(name, out var cats)
            && cats.Contains(selectedCategory, StringComparer.OrdinalIgnoreCase);

        var candidates = _allDistinctItemNames
            .Where(name => !existingItemNames.Contains(name)
                        && !alreadyAdding.Contains(name)
                        && !string.Equals(name, searchToken, StringComparison.OrdinalIgnoreCase));

        var startsWithMatches = candidates
            .Where(name => name.StartsWith(searchToken, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(IsSameCategory)
            .Take(maxResults)
            .ToList();

        if (startsWithMatches.Count >= maxResults)
        {
            return startsWithMatches;
        }

        var containsMatches = candidates
            .Where(name => name.Contains(searchToken, StringComparison.OrdinalIgnoreCase)
                        && !name.StartsWith(searchToken, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(IsSameCategory)
            .Take(maxResults - startsWithMatches.Count);

        return [.. startsWithMatches, .. containsMatches];
    }
}
