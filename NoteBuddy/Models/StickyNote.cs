namespace NoteBuddy.Models;

public class StickyNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Color { get; set; } = "yellow";
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public List<NoteItem> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
