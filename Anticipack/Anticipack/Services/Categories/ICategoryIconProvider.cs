using Anticipack.Packing;

namespace Anticipack.Services.Categories;

/// <summary>
/// Provides category icons (OCP: Open for extension via dictionary, closed for modification)
/// </summary>
public interface ICategoryIconProvider
{
    string GetIcon(PackingCategory category);
    string GetIcon(string categoryName);
}

/// <summary>
/// Default implementation using a dictionary for easy extension
/// </summary>
public sealed class CategoryIconProvider : ICategoryIconProvider
{
    private static readonly Dictionary<PackingCategory, string> CategoryIcons = new()
    {
        { PackingCategory.Clothing, "fa-tshirt" },
        { PackingCategory.Shoes, "fa-shoe-prints" },
        { PackingCategory.Toiletries, "fa-toothbrush" },
        { PackingCategory.Electronics, "fa-laptop" },
        { PackingCategory.Documents, "fa-passport" },
        { PackingCategory.Health, "fa-medkit" },
        { PackingCategory.Accessories, "fa-glasses" },
        { PackingCategory.Outdoor, "fa-hiking" },
        { PackingCategory.Food, "fa-utensils" },
        { PackingCategory.Entertainment, "fa-gamepad" },
        { PackingCategory.Miscellaneous, "fa-box" }
    };

    private const string DefaultIcon = "fa-box";

    public string GetIcon(PackingCategory category)
    {
        return CategoryIcons.GetValueOrDefault(category, DefaultIcon);
    }

    public string GetIcon(string categoryName)
    {
        if (Enum.TryParse<PackingCategory>(categoryName, ignoreCase: true, out var category))
        {
            return GetIcon(category);
        }
        return DefaultIcon;
    }
}
