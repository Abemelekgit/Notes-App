namespace NotesApp.Desktop.Models;

public record Note(
    Guid Id,
    string Title,
    string Body,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public string TagsText => string.Join(", ", Tags);
}

public record NoteInput(string Title, string Body, List<string> Tags);
