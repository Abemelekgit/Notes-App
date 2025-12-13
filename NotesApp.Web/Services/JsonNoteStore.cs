using System.Text.Json;
using NotesApp.Web.Models;

namespace NotesApp.Web.Services;

public interface INoteStore
{
    Task<List<Note>> SearchAsync(string? query);
    Task<Note?> GetAsync(Guid id);
    Task<Note> AddAsync(NoteInput input);
    Task<Note?> UpdateAsync(Guid id, NoteInput input);
    Task<bool> DeleteAsync(Guid id);
}

public class JsonNoteStore : INoteStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };
    private readonly SemaphoreSlim _mutex = new(1, 1);

    public JsonNoteStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<List<Note>> SearchAsync(string? query)
    {
        var notes = await LoadAsync();
        var normalizedQuery = query?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery)) return OrderNotes(notes);

        return OrderNotes(notes
            .Where(n => Contains(n.Title, normalizedQuery) || Contains(n.Body, normalizedQuery) || n.Tags.Any(t => Contains(t, normalizedQuery))));
    }

    public async Task<Note?> GetAsync(Guid id)
    {
        var notes = await LoadAsync();
        return notes.FirstOrDefault(n => n.Id == id);
    }

    public async Task<Note> AddAsync(NoteInput input)
    {
        var now = DateTime.UtcNow;
        var note = new Note(
            Guid.NewGuid(),
            input.Title.Trim(),
            (input.Body ?? string.Empty).Trim(),
            NormalizeTags(input.Tags),
            now,
            now);
        var notes = await LoadAsync();
        notes.Add(note);
        await SaveAsync(notes);
        return note;
    }

    public async Task<Note?> UpdateAsync(Guid id, NoteInput input)
    {
        var notes = await LoadAsync();
        var index = notes.FindIndex(n => n.Id == id);
        if (index < 0) return null;

        var existing = notes[index];
        var updated = existing with
        {
            Title = input.Title.Trim(),
            Body = (input.Body ?? string.Empty).Trim(),
            Tags = NormalizeTags(input.Tags),
            UpdatedAt = DateTime.UtcNow
        };

        notes[index] = updated;
        await SaveAsync(notes);
        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var notes = await LoadAsync();
        var removed = notes.RemoveAll(n => n.Id == id) > 0;
        if (removed)
        {
            await SaveAsync(notes);
        }
        return removed;
    }

    private async Task<List<Note>> LoadAsync()
    {
        await _mutex.WaitAsync();
        try
        {
            if (!File.Exists(_filePath)) return new List<Note>();
            var json = await File.ReadAllTextAsync(_filePath);
            var loaded = JsonSerializer.Deserialize<List<Note>>(json, _jsonOptions);
            return OrderNotes(loaded ?? new List<Note>());
        }
        catch
        {
            return new List<Note>();
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task SaveAsync(List<Note> notes)
    {
        await _mutex.WaitAsync();
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(_filePath))
            {
                var backupPath = _filePath + ".bak";
                File.Copy(_filePath, backupPath, overwrite: true);
            }

            var json = JsonSerializer.Serialize(notes, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private static List<string> NormalizeTags(List<string> tags) => tags
        .Where(t => !string.IsNullOrWhiteSpace(t))
        .Select(t => t.Trim())
        .Where(t => t.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static bool Contains(string source, string query) =>
        source?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

    private static List<Note> OrderNotes(IEnumerable<Note> notes) => notes
        .OrderByDescending(n => n.UpdatedAt)
        .ThenBy(n => n.Title, StringComparer.OrdinalIgnoreCase)
        .ToList();
}
