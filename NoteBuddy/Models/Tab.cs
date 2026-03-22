namespace NoteBuddy.Models;

/// <summary>
/// Represents a tab (board) on the corkboard that contains its own set of sticky notes and pictures.
/// </summary>
public class Tab
{
    /// <summary>
    /// Gets or sets the unique identifier for this tab.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display title of the tab.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the color theme of the tab (e.g., "yellow", "blue", "green", "orange", "pink", "purple").
    /// </summary>
    public string Color { get; set; } = "yellow";

    /// <summary>
    /// Gets or sets the sort order for positioning this tab in the tab row.
    /// Lower values appear further left.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the sticky notes contained within this tab.
    /// </summary>
    public List<StickyNote> Notes { get; set; } = new();

    /// <summary>
    /// Gets or sets the pinned pictures contained within this tab.
    /// </summary>
    public List<PinnedPicture> Pictures { get; set; } = new();
}
