namespace NoteBuddy.Models;

/// <summary>
/// Root data model that holds all tabs and their content on the corkboard.
/// The Notes and Pictures properties are retained for backward compatibility
/// with pre-tabs JSON files; they are migrated into a default tab on load.
/// </summary>
public class CorkboardData
{
    /// <summary>
    /// Gets or sets the collection of tabs, each containing its own notes and pictures.
    /// </summary>
    public List<Tab> Tabs { get; set; } = new();

    /// <summary>
    /// Legacy: flat list of sticky notes from pre-tabs format. Used only for migration.
    /// </summary>
    public List<StickyNote>? Notes { get; set; }

    /// <summary>
    /// Legacy: flat list of pictures from pre-tabs format. Used only for migration.
    /// </summary>
    public List<PinnedPicture>? Pictures { get; set; }
}
