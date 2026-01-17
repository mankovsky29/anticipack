namespace Anticipack.Components.Shared.NavigationHeaderComponent
{
    public interface INavigationHeaderService
    {
        /// <summary>
        /// Event fired when the text changes
        /// </summary>
        event Action<string>? OnTextChanged;

        /// <summary>
        /// Event fired when the navigation menu is toggled
        /// </summary>
        event Action<bool>? OnNavMenuToggled;

        /// <summary>
        /// Sets the text to be displayed in the navigation header
        /// </summary>
        void SetText(string text);

        /// <summary>
        /// Gets the current text displayed in the navigation header
        /// </summary>
        string GetText();

        /// <summary>
        /// Sets the navigation menu expanded state
        /// </summary>
        void SetNavMenuExpanded(bool isExpanded);

        /// <summary>
        /// Gets the navigation menu expanded state
        /// </summary>
        bool IsNavMenuExpanded();
    }
}
