using System.Text.Json;
using NoteBuddy.Models;

namespace NoteBuddy.Services;

/// <summary>
/// Singleton service that manages corkboard state (sticky notes and pinned pictures)
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
    /// Data is stored in %APPDATA%\NoteBuddy\ so it survives upgrades and works with standard user permissions.
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

    /// <summary>Gets the entire corkboard data including all notes and pictures.</summary>
    public CorkboardData GetData() => _data;

    /// <summary>Gets the list of all sticky notes on the corkboard.</summary>
    public List<StickyNote> GetNotes() => _data.Notes;

    /// <summary>Gets the list of all pinned pictures on the corkboard.</summary>
    public List<PinnedPicture> GetPictures() => _data.Pictures;

    /// <summary>Adds a new sticky note to the corkboard and persists the change.</summary>
    /// <param name="note">The sticky note to add.</param>
    public async Task AddNoteAsync(StickyNote note)
    {
        await _lock.WaitAsync();
        try
        {
            _data.Notes.Add(note);
            await SaveDataAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Replaces an existing sticky note with updated data and persists the change.</summary>
    /// <param name="updatedNote">The note containing updated values; matched by <see cref="StickyNote.Id"/>.</param>
    public async Task UpdateNoteAsync(StickyNote updatedNote)
    {
        await _lock.WaitAsync();
        try
        {
            var index = _data.Notes.FindIndex(n => n.Id == updatedNote.Id);
            if (index >= 0)
            {
                _data.Notes[index] = updatedNote;
                await SaveDataAsync();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Removes a sticky note from the corkboard and persists the change.</summary>
    /// <param name="noteId">The unique identifier of the note to delete.</param>
    public async Task DeleteNoteAsync(Guid noteId)
    {
        await _lock.WaitAsync();
        try
        {
            _data.Notes.RemoveAll(n => n.Id == noteId);
            await SaveDataAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Updates the position of a sticky note on the corkboard and persists the change.</summary>
    /// <param name="noteId">The unique identifier of the note to reposition.</param>
    /// <param name="x">The new horizontal position.</param>
    /// <param name="y">The new vertical position.</param>
    public async Task UpdateNotePositionAsync(Guid noteId, double x, double y)
    {
        await _lock.WaitAsync();
        try
        {
            var note = _data.Notes.FirstOrDefault(n => n.Id == noteId);
            if (note != null)
            {
                note.PositionX = x;
                note.PositionY = y;
                await SaveDataAsync();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Adds a new pinned picture to the corkboard and persists the change.</summary>
    /// <param name="picture">The pinned picture to add.</param>
    public async Task AddPictureAsync(PinnedPicture picture)
    {
        await _lock.WaitAsync();
        try
        {
            _data.Pictures.Add(picture);
            await SaveDataAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Deletes a pinned picture from the corkboard, removes its uploaded file from disk, and persists the change.</summary>
    /// <param name="pictureId">The unique identifier of the picture to delete.</param>
    public async Task DeletePictureAsync(Guid pictureId)
    {
        await _lock.WaitAsync();
        try
        {
            var picture = _data.Pictures.FirstOrDefault(p => p.Id == pictureId);
            if (picture != null)
            {
                // Delete the uploaded file
                var filePath = Path.Combine(_uploadsPath, picture.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                _data.Pictures.RemoveAll(p => p.Id == pictureId);
                await SaveDataAsync();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Updates the position of a pinned picture on the corkboard and persists the change.</summary>
    /// <param name="pictureId">The unique identifier of the picture to reposition.</param>
    /// <param name="x">The new horizontal position.</param>
    /// <param name="y">The new vertical position.</param>
    public async Task UpdatePicturePositionAsync(Guid pictureId, double x, double y)
    {
        await _lock.WaitAsync();
        try
        {
            var picture = _data.Pictures.FirstOrDefault(p => p.Id == pictureId);
            if (picture != null)
            {
                picture.PositionX = x;
                picture.PositionY = y;
                await SaveDataAsync();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Gets the absolute file-system path to the uploads directory.</summary>
    public string GetUploadsPath() => _uploadsPath;

    /// <summary>Loads corkboard data from the JSON file on disk, or initializes empty data if the file does not exist.</summary>
    private void LoadData()
    {
        if (File.Exists(_dataFilePath))
        {
            var json = File.ReadAllText(_dataFilePath);
            _data = JsonSerializer.Deserialize<CorkboardData>(json, JsonOptions) ?? new CorkboardData();
        }
    }

    /// <summary>Serializes the current corkboard data to the JSON file on disk.</summary>
    private async Task SaveDataAsync()
    {
        var json = JsonSerializer.Serialize(_data, JsonOptions);
        await File.WriteAllTextAsync(_dataFilePath, json);
    }
}
