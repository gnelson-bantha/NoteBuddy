namespace NoteBuddy.Models;

/// <summary>
/// Represents a picture pinned to the corkboard.
/// </summary>
public class PinnedPicture
{
    /// <summary>
    /// Gets or sets the unique identifier for this pinned picture.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the file name of the picture image.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the horizontal position of the picture on the corkboard.
    /// </summary>
    public double PositionX { get; set; }

    /// <summary>
    /// Gets or sets the vertical position of the picture on the corkboard.
    /// </summary>
    public double PositionY { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this picture was pinned.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
