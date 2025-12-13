using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NotesApp.Desktop.Models;
using NotesApp.Desktop.Services;

namespace NotesApp.Desktop.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly INoteStore _store;

    public ObservableCollection<Note> Notes { get; } = new();

    private Note? _selectedNote;
    public Note? SelectedNote
    {
        get => _selectedNote;
        set
        {
            if (_selectedNote != value)
            {
                _selectedNote = value;
                OnPropertyChanged();
                PopulateFormFromSelection();
            }
        }
    }

    private string _titleInput = string.Empty;
    public string TitleInput
    {
        get => _titleInput;
        set { _titleInput = value; OnPropertyChanged(); }
    }

    private string _bodyInput = string.Empty;
    public string BodyInput
    {
        get => _bodyInput;
        set { _bodyInput = value; OnPropertyChanged(); }
    }

    private string _tagsInput = string.Empty;
    public string TagsInput
    {
        get => _tagsInput;
        set { _tagsInput = value; OnPropertyChanged(); }
    }

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set { _searchQuery = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel(INoteStore store)
    {
        _store = store;
    }

    public async Task LoadAsync()
    {
        await SearchAsync();
    }

    public async Task SearchAsync()
    {
        var results = await _store.SearchAsync(SearchQuery);
        ApplyNotes(results);
        StatusMessage = $"Loaded {Notes.Count} note(s).";
    }

    public async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(TitleInput))
        {
            StatusMessage = "Title is required.";
            return;
        }

        var input = BuildInput();
        var created = await _store.AddAsync(input);
        await SearchAsync();
        SelectedNote = Notes.FirstOrDefault(n => n.Id == created.Id);
        StatusMessage = "Note added.";
    }

    public async Task UpdateAsync()
    {
        if (SelectedNote is null)
        {
            StatusMessage = "Select a note to update.";
            return;
        }

        if (string.IsNullOrWhiteSpace(TitleInput))
        {
            StatusMessage = "Title is required.";
            return;
        }

        var updated = await _store.UpdateAsync(SelectedNote.Id, BuildInput());
        if (updated is null)
        {
            StatusMessage = "Note not found anymore.";
            return;
        }

        await SearchAsync();
        SelectedNote = Notes.FirstOrDefault(n => n.Id == updated.Id);
        StatusMessage = "Note updated.";
    }

    public async Task DeleteAsync()
    {
        if (SelectedNote is null)
        {
            StatusMessage = "Select a note to delete.";
            return;
        }

        var deleted = await _store.DeleteAsync(SelectedNote.Id);
        if (!deleted)
        {
            StatusMessage = "Note not found anymore.";
            return;
        }

        await SearchAsync();
        ClearForm();
        StatusMessage = "Note deleted.";
    }

    public void ClearForm()
    {
        SelectedNote = null;
        TitleInput = string.Empty;
        BodyInput = string.Empty;
        TagsInput = string.Empty;
    }

    private NoteInput BuildInput() => new(
        TitleInput.Trim(),
        BodyInput,
        SplitTags(TagsInput));

    private void ApplyNotes(List<Note> notes)
    {
        Notes.Clear();
        foreach (var note in notes)
        {
            Notes.Add(note);
        }
    }

    private void PopulateFormFromSelection()
    {
        if (SelectedNote is null)
        {
            return;
        }

        TitleInput = SelectedNote.Title;
        BodyInput = SelectedNote.Body;
        TagsInput = string.Join(", ", SelectedNote.Tags);
    }

    private static List<string> SplitTags(string input) => input
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(t => t.Trim())
        .Where(t => t.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
