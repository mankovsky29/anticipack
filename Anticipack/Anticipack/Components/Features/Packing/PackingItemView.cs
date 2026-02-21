namespace Anticipack.Components.Features.Packing;

/// <summary>
/// View model wrapping a <see cref="Storage.PackingItem"/> for display in packing lists.
/// Shared by EditPacking and PlayPacking pages.
/// </summary>
internal sealed class PackingItemView
{
    public PackingItemView(Storage.PackingItem item) => Item = item;

    public Storage.PackingItem Item { get; }
    public bool IsChecked { get; set; }

    /// <summary>
    /// Used by PlayPacking to track the vanishing animation state.
    /// </summary>
    public bool IsAnimating { get; set; }
}
