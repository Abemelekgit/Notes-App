namespace NotesApp.Web.Models;

public record Note(
    Guid Id,
    string Title,
    string Body,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record NoteInput(string Title, string Body, List<string> Tags);
