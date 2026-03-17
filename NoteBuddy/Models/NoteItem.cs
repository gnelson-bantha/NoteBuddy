namespace NoteBuddy.Models;

/// <summary>
/// Represents a single checklist item on a sticky note.
/// </summary>
public class NoteItem
{
    /// <summary>
    /// Gets or sets the unique identifier for this checklist item.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display text of the checklist item.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this item has been checked off.
    /// </summary>
    public bool IsChecked { get; set; }
}
