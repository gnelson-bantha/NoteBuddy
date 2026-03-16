namespace NoteBuddy.Models;

public class NoteItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public bool IsChecked { get; set; }
}
