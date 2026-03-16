using System.Text.Json;
using NoteBuddy.Models;

namespace NoteBuddy.Services;

public class CorkboardService
{
    private readonly string _dataFilePath;
    private readonly string _uploadsPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private CorkboardData _data = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CorkboardService(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDir);
        _dataFilePath = Path.Combine(dataDir, "corkboard.json");

        _uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(_uploadsPath);

        LoadData();
    }

    public CorkboardData GetData() => _data;

    public List<StickyNote> GetNotes() => _data.Notes;

    public List<PinnedPicture> GetPictures() => _data.Pictures;

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

    public string GetUploadsPath() => _uploadsPath;

    private void LoadData()
    {
        if (File.Exists(_dataFilePath))
        {
            var json = File.ReadAllText(_dataFilePath);
            _data = JsonSerializer.Deserialize<CorkboardData>(json, JsonOptions) ?? new CorkboardData();
        }
    }

    private async Task SaveDataAsync()
    {
        var json = JsonSerializer.Serialize(_data, JsonOptions);
        await File.WriteAllTextAsync(_dataFilePath, json);
    }
}
