namespace NoteBuddy.Models;

/// <summary>
/// Represents a sticky note on the corkboard with a title, color, position, and checklist items.
/// </summary>
public class StickyNote
{
    /// <summary>
    /// Gets or sets the unique identifier for this sticky note.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the title displayed at the top of the sticky note.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the background color of the sticky note (e.g., "yellow", "pink").
    /// </summary>
    public string Color { get; set; } = "yellow";

    /// <summary>
    /// Gets or sets the horizontal position of the note on the corkboard.
    /// </summary>
    public double PositionX { get; set; }

    /// <summary>
    /// Gets or sets the vertical position of the note on the corkboard.
    /// </summary>
    public double PositionY { get; set; }

    /// <summary>
    /// Gets or sets the scale factor for the sticky note (1.0 = 325×324px, 2.0 = 650×648px).
    /// Aspect ratio is locked so the background image does not warp.
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the checklist items contained within this sticky note.
    /// </summary>
    public List<NoteItem> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the UTC timestamp when this sticky note was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
