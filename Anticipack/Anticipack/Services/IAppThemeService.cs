namespace Anticipack.Services
{
    /// <summary>
    /// Keeps native chrome (status/navigation bar icons, iOS interface style)
    /// in sync with the theme chosen inside the Blazor UI.
    /// </summary>
    public interface IAppThemeService
    {
        /// <param name="theme">"light", "dark" or "auto" (follow the system)</param>
        void Apply(string theme);
    }
}
