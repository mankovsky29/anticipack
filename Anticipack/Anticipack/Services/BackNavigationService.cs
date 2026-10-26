using Microsoft.AspNetCore.Components;

namespace Anticipack.Services;

/// <summary>
/// Back with no history goes to Insights first and only exits the app from there.
/// </summary>
public sealed class BackNavigationService : IBackNavigationService
{
    private const string FallbackRoute = "statistics";

    private NavigationManager? _navigation;

    /// <inheritdoc/>
    public void Attach(NavigationManager navigation)
    {
        _navigation = navigation;
    }

    /// <inheritdoc/>
    public bool TryHandleBack()
    {
        if (_navigation is null)
            return false;

        var path = _navigation.ToBaseRelativePath(_navigation.Uri).Split('?', '#')[0].Trim('/');
        if (string.Equals(path, FallbackRoute, StringComparison.OrdinalIgnoreCase))
            return false;

        // Replace so the next back press on Insights has no history and exits
        _navigation.NavigateTo($"/{FallbackRoute}", replace: true);
        return true;
    }
}
