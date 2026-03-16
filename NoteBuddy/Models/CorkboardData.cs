namespace NoteBuddy.Models;

public class CorkboardData
{
    public List<StickyNote> Notes { get; set; } = new();
    public List<PinnedPicture> Pictures { get; set; } = new();
}
