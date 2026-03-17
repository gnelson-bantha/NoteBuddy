namespace NoteBuddy.Models;

/// <summary>
/// Root data model that holds all notes and pictures displayed on the corkboard.
/// </summary>
public class CorkboardData
{
    /// <summary>
    /// Gets or sets the collection of sticky notes on the corkboard.
    /// </summary>
    public List<StickyNote> Notes { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of pictures pinned to the corkboard.
    /// </summary>
    public List<PinnedPicture> Pictures { get; set; } = new();
}
