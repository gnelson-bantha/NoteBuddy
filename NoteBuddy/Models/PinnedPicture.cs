namespace NoteBuddy.Models;

public class PinnedPicture
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
