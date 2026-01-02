namespace Anticipack.Components.Shared.SidebarTextComponent
{
    public interface ISidebarTextService
    {
        /// <summary>
        /// Sets the text to be displayed in the sidebar
        /// </summary>
        void SetText(string text);

        /// <summary>
        /// Gets the current text displayed in the sidebar
        /// </summary>
        string GetText();

        /// <summary>
        /// Sets the packing ID for editing when text is clicked
        /// </summary>
        void SetPackingId(string? packingId);

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
