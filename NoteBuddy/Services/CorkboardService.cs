using System.Text.Json;
using NoteBuddy.Models;

namespace NoteBuddy.Services;

/// <summary>
/// Singleton service that manages corkboard state (tabs, sticky notes, and pinned pictures)
/// with JSON file persistence and thread-safe access.
/// </summary>
public class CorkboardService
{
    /// <summary>Path to the JSON file used for persisting corkboard data.</summary>
    private readonly string _dataFilePath;
    /// <summary>Path to the directory where uploaded picture files are stored.</summary>
    private readonly string _uploadsPath;
    /// <summary>Semaphore ensuring thread-safe read/write access to corkboard data.</summary>
    private readonly SemaphoreSlim _lock = new(1, 1);
    /// <summary>In-memory representation of the current corkboard state.</summary>
    private CorkboardData _data = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes the corkboard service, ensuring data and upload directories exist, then loads persisted data.
    /// </summary>
    public CorkboardService()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NoteBuddy");
        Directory.CreateDirectory(appDataDir);

        _dataFilePath = Path.Combine(appDataDir, "corkboard.json");

        _uploadsPath = Path.Combine(appDataDir, "uploads");
        Directory.CreateDirectory(_uploadsPath);

        LoadData();
    }

    // ===== Tab Methods =====

    /// <summary>Gets all tabs ordered by SortOrder.</summary>
    public List<Tab> GetTabs() => _data.Tabs.OrderBy(t => t.SortOrder).ToList();

    /// <summary>Gets a specific tab by its ID.</summary>
    public Tab? GetTab(Guid tabId) => _data.Tabs.FirstOrDefault(t => t.Id == tabId);

    /// <summary>Adds a new tab and persists the change.</summary>
    public async Task AddTabAsync(Tab tab)
    {
        await _lock.WaitAsync();
        try
        {
            if (!_data.Tabs.Any())
                tab.SortOrder = 0;
            else
                tab.SortOrder = _data.Tabs.Max(t => t.SortOrder) + 1;

            _data.Tabs.Add(tab);
            await SaveDataAsync();
        }
        finally { _lock.Release(); }
    }

    /// <summary>Updates a tab's properties (title, color) and persists the change.</summary>
    public async Task UpdateTabAsync(Tab updatedTab)
    {
        await _lock.WaitAsync();
        try
        {
            var index = _data.Tabs.FindIndex(t => t.Id == updatedTab.Id);
            if (index >= 0)
            {
                _data.Tabs[index] = updatedTab;
                await SaveDataAsync();
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>Deletes a tab and all its notes and pictures (including uploaded files).</summary>
    public async Task DeleteTabAsync(Guid tabId)
    {
        await _lock.WaitAsync();
        try
        {
            var tab = _data.Tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab != null)
            {
                // Delete uploaded picture files
                foreach (var pic in tab.Pictures)
                {
                    var filePath = Path.Combine(_uploadsPath, pic.FileName);
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
                _data.Tabs.Remove(tab);
                await SaveDataAsync();
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>Reorders tabs by updating their SortOrder values and persists the change.</summary>
    public async Task ReorderTabsAsync(List<Guid> tabIdsInOrder)
    {
        await _lock.WaitAsync();
        try
        {
            for (int i = 0; i < tabIdsInOrder.Count; i++)
            {
                var tab = _data.Tabs.FirstOrDefault(t => t.Id == tabIdsInOrder[i]);
                if (tab != null) tab.SortOrder = i;
            }
            await SaveDataAsync();
        }
        finally { _lock.Release(); }
    }

    // ===== Note Methods (tab-aware) =====

    /// <summary>Gets the notes for a specific tab.</summary>
    public List<StickyNote> GetNotes(Guid tabId) =>
        _data.Tabs.FirstOrDefault(t => t.Id == tabId)?.Notes ?? new();

    /// <summary>Gets all notes across all tabs (for reminders).</summary>
    public List<StickyNote> GetAllNotes() =>
        _data.Tabs.SelectMany(t => t.Notes).ToList();

    /// <summary>Adds a note to a specific tab.</summary>
    public async Task AddNoteAsync(Guid tabId, StickyNote note)
    {
        await _lock.WaitAsync();
        try
        {
            var tab = _data.Tabs.FirstOrDefault(t => t.Id == tabId);
            tab?.Notes.Add(note);
            await SaveDataAsync();
        }
        finally { _lock.Release(); }
    }

    /// <summary>Updates a note (searches across all tabs).</summary>
    public async Task UpdateNoteAsync(StickyNote updatedNote)
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var tab in _data.Tabs)
            {
                var index = tab.Notes.FindIndex(n => n.Id == updatedNote.Id);
                if (index >= 0)
                {
                    tab.Notes[index] = updatedNote;
                    await SaveDataAsync();
                    return;
                }
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>Deletes a note (searches across all tabs).</summary>
    public async Task DeleteNoteAsync(Guid noteId)
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var tab in _data.Tabs)
            {
                if (tab.Notes.RemoveAll(n => n.Id == noteId) > 0)
                {
                    await SaveDataAsync();
                    return;
                }
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>Updates a note's position (searches across all tabs).</summary>
    public async Task UpdateNotePositionAsync(Guid noteId, double x, double y)
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var tab in _data.Tabs)
            {
                var note = tab.Notes.FirstOrDefault(n => n.Id == noteId);
                if (note != null)
                {
                    note.PositionX = x;
                    note.PositionY = y;
                    await SaveDataAsync();
                    return;
                }
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>Moves a note from its current tab to the target tab.</summary>
    public async Task MoveNoteToTabAsync(Guid noteId, Guid targetTabId)
    {
        await _lock.WaitAsync();
        try
        {
            StickyNote? note = null;
            foreach (var tab in _data.Tabs)
            {
                note = tab.Notes.FirstOrDefault(n => n.Id == noteId);
                if (note != null)
                {
                    tab.Notes.Remove(note);
                    break;
                }
            }
            if (note != null)
            {
                var targetTab = _data.Tabs.FirstOrDefault(t => t.Id == targetTabId);
                if (targetTab != null)
                {
                    // Assign a fresh position in the target tab
                    var count = targetTab.Notes.Count + targetTab.Pictures.Count;
                    note.PositionX = 20 + (count % 5) * 345;
                    note.PositionY = 14 + (count / 5) * 345;
                    targetTab.Notes.Add(note);
                }
                await SaveDataAsync();
            }
        }
        finally { _lock.Release(); }
    }

    // ===== Picture Methods (tab-aware) =====

    /// <summary>Gets the pictures for a specific tab.</summary>
    public List<PinnedPicture> GetPictures(Guid tabId) =>
        _data.Tabs.FirstOrDefault(t => t.Id == tabId)?.Pictures ?? new();

    /// <summary>Adds a picture to a specific tab.</summary>
    public async Task AddPictureAsync(Guid tabId, PinnedPicture picture)
    {
        await _lock.WaitAsync();
        try
        {
            var tab = _data.Tabs.FirstOrDefault(t => t.Id == tabId);
            tab?.Pictures.Add(picture);
            await SaveDataAsync();
        }
        finally { _lock.Release(); }
    }

    /// <summary>Deletes a picture (searches across all tabs) and removes its file.</summary>
    public async Task DeletePictureAsync(Guid pictureId)
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var tab in _data.Tabs)
            {
                var picture = tab.Pictures.FirstOrDefault(p => p.Id == pictureId);
                if (picture != null)
                {
                    var filePath = Path.Combine(_uploadsPath, picture.FileName);
                    if (File.Exists(filePath)) File.Delete(filePath);
                    tab.Pictures.Remove(picture);
                    await SaveDataAsync();
                    return;
                }
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>Updates a picture's position (searches across all tabs).</summary>
    public async Task UpdatePicturePositionAsync(Guid pictureId, double x, double y)
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var tab in _data.Tabs)
            {
                var pic = tab.Pictures.FirstOrDefault(p => p.Id == pictureId);
                if (pic != null)
                {
                    pic.PositionX = x;
                    pic.PositionY = y;
                    await SaveDataAsync();
                    return;
                }
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>Updates a picture's properties (searches across all tabs).</summary>
    public async Task UpdatePictureAsync(PinnedPicture updatedPicture)
    {
        await _lock.WaitAsync();
        try
        {
            foreach (var tab in _data.Tabs)
            {
                var index = tab.Pictures.FindIndex(p => p.Id == updatedPicture.Id);
                if (index >= 0)
                {
                    tab.Pictures[index] = updatedPicture;
                    await SaveDataAsync();
                    return;
                }
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>Moves a picture from its current tab to the target tab.</summary>
    public async Task MovePictureToTabAsync(Guid pictureId, Guid targetTabId)
    {
        await _lock.WaitAsync();
        try
        {
            PinnedPicture? pic = null;
            foreach (var tab in _data.Tabs)
            {
                pic = tab.Pictures.FirstOrDefault(p => p.Id == pictureId);
                if (pic != null)
                {
                    tab.Pictures.Remove(pic);
                    break;
                }
            }
            if (pic != null)
            {
                var targetTab = _data.Tabs.FirstOrDefault(t => t.Id == targetTabId);
                if (targetTab != null)
                {
                    var count = targetTab.Notes.Count + targetTab.Pictures.Count;
                    pic.PositionX = 20 + (count % 5) * 345;
                    pic.PositionY = 14 + (count / 5) * 345;
                    targetTab.Pictures.Add(pic);
                }
                await SaveDataAsync();
            }
        }
        finally { _lock.Release(); }
    }

    /// <summary>Gets the absolute file-system path to the uploads directory.</summary>
    public string GetUploadsPath() => _uploadsPath;

    /// <summary>
    /// Loads corkboard data from the JSON file. If the file uses the legacy format
    /// (flat Notes/Pictures at root), migrates content into a default "My Board" tab.
    /// </summary>
    private void LoadData()
    {
        if (File.Exists(_dataFilePath))
        {
            var json = File.ReadAllText(_dataFilePath);
            _data = JsonSerializer.Deserialize<CorkboardData>(json, JsonOptions) ?? new CorkboardData();

            // Migrate legacy format: flat Notes/Pictures → default tab
            if (_data.Tabs.Count == 0 && (_data.Notes?.Count > 0 || _data.Pictures?.Count > 0))
            {
                var defaultTab = new Tab
                {
                    Title = "My Board",
                    Color = "yellow",
                    SortOrder = 0,
                    Notes = _data.Notes ?? new(),
                    Pictures = _data.Pictures ?? new()
                };
                _data.Tabs.Add(defaultTab);
                _data.Notes = null;
                _data.Pictures = null;

                // Persist migration immediately
                var migrated = JsonSerializer.Serialize(_data, JsonOptions);
                File.WriteAllText(_dataFilePath, migrated);
            }
        }

        // Ensure at least one tab exists
        if (_data.Tabs.Count == 0)
        {
            _data.Tabs.Add(new Tab
            {
                Title = "My Board",
                Color = "yellow",
                SortOrder = 0
            });
        }
    }

    /// <summary>Serializes the current corkboard data to the JSON file on disk.</summary>
    private async Task SaveDataAsync()
    {
        // Clear legacy fields before saving
        _data.Notes = null;
        _data.Pictures = null;
        var json = JsonSerializer.Serialize(_data, JsonOptions);
        await File.WriteAllTextAsync(_dataFilePath, json);
    }
}
