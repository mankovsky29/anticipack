using Microsoft.AspNetCore.Components;

namespace Anticipack.Services;

/// <summary>
/// Handles the system back button once the web view has no history left to go back to.
/// </summary>
public interface IBackNavigationService
{
    /// <summary>Connects the Blazor navigation manager once the app's routes are running.</summary>
    void Attach(NavigationManager navigation);

    /// <summary>
    /// Navigates to the fallback screen when the user isn't on it yet.
    /// Returns false when the app should handle back itself (exit).
    /// </summary>
    bool TryHandleBack();
}
