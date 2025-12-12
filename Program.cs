using System.Text.Json;

var app = new NotesApp(Path.Combine(AppContext.BaseDirectory, "notes.json"));
app.Run();

record Note(
	Guid Id,
	string Title,
	string Body,
	List<string> Tags,
	DateTime CreatedAt,
	DateTime UpdatedAt
);

class NotesApp
{
	private readonly string _filePath;
	private readonly List<Note> _notes;
	private readonly JsonSerializerOptions _jsonOptions = new()
	{
		WriteIndented = true
	};

	public NotesApp(string filePath)
	{
		_filePath = filePath;
		_notes = LoadNotes();
	}

	public void Run()
	{
		Console.WriteLine("NotesApp (.NET 8)\n------------------");
		var exitRequested = false;

		while (!exitRequested)
		{
			ShowMenu();
			Console.Write("Select an option: ");
			var choice = (Console.ReadLine() ?? string.Empty).Trim();

			switch (choice)
			{
				case "1":
					AddNote();
					break;
				case "2":
					ListNotes();
					break;
				case "3":
					ViewNote();
					break;
				case "4":
					SearchNotes();
					break;
				case "5":
					EditNote();
					break;
				case "6":
					DeleteNote();
					break;
				case "7":
					SaveNotes();
					exitRequested = true;
					break;
				default:
					Console.WriteLine("Unknown option. Please try again.");
					break;
			}
		}

		Console.WriteLine("Goodbye!");
	}

	private void ShowMenu()
	{
		Console.WriteLine();
		Console.WriteLine("1) Add note");
		Console.WriteLine("2) List notes (sorted by last updated)");
		Console.WriteLine("3) View note (by number)");
		Console.WriteLine("4) Search notes (title/body/tags)");
		Console.WriteLine("5) Edit note");
		Console.WriteLine("6) Delete note");
		Console.WriteLine("7) Save & Exit");
	}

	private void AddNote()
	{
		Console.WriteLine("\nAdd note");
		var title = ReadRequired("Title: ");
		var body = ReadMultiline("Body (finish with empty line): ");
		var tags = ReadTags();

		var now = DateTime.UtcNow;
		_notes.Add(new Note(Guid.NewGuid(), title, body, tags, now, now));
		SaveNotes();
		Console.WriteLine("Note added.");
	}

	private void ListNotes()
	{
		if (_notes.Count == 0)
		{
			Console.WriteLine("No notes yet.");
			return;
		}

		Console.WriteLine("\nNotes (newest first):");
		var ordered = _notes
			.OrderByDescending(n => n.UpdatedAt)
			.ToList();

		for (var i = 0; i < ordered.Count; i++)
		{
			var note = ordered[i];
			Console.WriteLine($"{i + 1}. {note.Title} (updated {note.UpdatedAt:u}) [tags: {string.Join(", ", note.Tags)}]");
		}
	}

	private void ViewNote()
	{
		var (note, _) = SelectNote("view");
		if (note is null) return;

		Console.WriteLine($"\nTitle: {note.Title}");
		Console.WriteLine($"Created: {note.CreatedAt:u}");
		Console.WriteLine($"Updated: {note.UpdatedAt:u}");
		Console.WriteLine($"Tags: {string.Join(", ", note.Tags)}");
		Console.WriteLine("Body:\n" + note.Body);
	}

	private void SearchNotes()
	{
		Console.Write("Search text: ");
		var query = (Console.ReadLine() ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(query))
		{
			Console.WriteLine("Search cancelled.");
			return;
		}

		var results = _notes
			.OrderByDescending(n => n.UpdatedAt)
			.Where(n => Contains(n.Title, query) || Contains(n.Body, query) || n.Tags.Any(t => Contains(t, query)))
			.ToList();

		if (results.Count == 0)
		{
			Console.WriteLine("No matches found.");
			return;
		}

		Console.WriteLine($"Found {results.Count} note(s):");
		for (var i = 0; i < results.Count; i++)
		{
			var note = results[i];
			Console.WriteLine($"{i + 1}. {note.Title} (updated {note.UpdatedAt:u}) [tags: {string.Join(", ", note.Tags)}]");
		}
	}

	private void EditNote()
	{
		var (note, index) = SelectNote("edit");
		if (note is null || index is null) return;

		Console.WriteLine("Press Enter to keep existing values.");
		var newTitle = ReadOptional($"Title ({note.Title}): ", note.Title);
		var newBody = ReadOptionalMultiline("Body (finish with empty line): ", note.Body);
		var newTags = ReadTags(prompt: "Tags comma-separated (leave blank to keep): ", fallback: note.Tags);

		_notes[index.Value] = note with
		{
			Title = newTitle,
			Body = newBody,
			Tags = newTags,
			UpdatedAt = DateTime.UtcNow
		};

		SaveNotes();
		Console.WriteLine("Note updated.");
	}

	private void DeleteNote()
	{
		var (note, index) = SelectNote("delete");
		if (note is null || index is null) return;

		Console.Write($"Type 'yes' to delete '{note.Title}': ");
		var confirm = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
		if (confirm == "yes")
		{
			_notes.RemoveAt(index.Value);
			SaveNotes();
			Console.WriteLine("Note deleted.");
		}
		else
		{
			Console.WriteLine("Delete cancelled.");
		}
	}

	private (Note? note, int? index) SelectNote(string action)
	{
		if (_notes.Count == 0)
		{
			Console.WriteLine("No notes to select.");
			return (null, null);
		}

		var ordered = _notes
			.OrderByDescending(n => n.UpdatedAt)
			.ToList();

		for (var i = 0; i < ordered.Count; i++)
		{
			var note = ordered[i];
			Console.WriteLine($"{i + 1}. {note.Title} (updated {note.UpdatedAt:u})");
		}

		Console.Write($"Select a note number to {action}: ");
		var input = (Console.ReadLine() ?? string.Empty).Trim();
		if (!int.TryParse(input, out var selection) || selection < 1 || selection > ordered.Count)
		{
			Console.WriteLine("Invalid selection.");
			return (null, null);
		}

		var noteToUse = ordered[selection - 1];
		var originalIndex = _notes.FindIndex(n => n.Id == noteToUse.Id);
		return (noteToUse, originalIndex);
	}

	private List<Note> LoadNotes()
	{
		try
		{
			if (!File.Exists(_filePath)) return new List<Note>();
			var json = File.ReadAllText(_filePath);
			var loaded = JsonSerializer.Deserialize<List<Note>>(json, _jsonOptions);
			return loaded ?? new List<Note>();
		}
		catch
		{
			Console.WriteLine("Could not read notes.json. Starting with an empty list.");
			return new List<Note>();
		}
	}

	private void SaveNotes()
	{
		var directory = Path.GetDirectoryName(_filePath);
		if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var json = JsonSerializer.Serialize(_notes, _jsonOptions);
		File.WriteAllText(_filePath, json);
	}

	private static string ReadRequired(string prompt)
	{
		while (true)
		{
			Console.Write(prompt);
			var input = (Console.ReadLine() ?? string.Empty).Trim();
			if (!string.IsNullOrWhiteSpace(input)) return input;
			Console.WriteLine("Value is required.");
		}
	}

	private static string ReadOptional(string prompt, string fallback)
	{
		Console.Write(prompt);
		var input = Console.ReadLine();
		return string.IsNullOrWhiteSpace(input) ? fallback : input.Trim();
	}

	private static string ReadMultiline(string prompt)
	{
		Console.WriteLine(prompt);
		var lines = new List<string>();
		while (true)
		{
			var line = Console.ReadLine();
			if (string.IsNullOrEmpty(line)) break;
			lines.Add(line);
		}

		return string.Join(Environment.NewLine, lines);
	}

	private static string ReadOptionalMultiline(string prompt, string fallback)
	{
		Console.WriteLine(prompt);
		var lines = new List<string>();
		while (true)
		{
			var line = Console.ReadLine();
			if (string.IsNullOrEmpty(line)) break;
			lines.Add(line);
		}

		if (lines.Count == 0) return fallback;
		return string.Join(Environment.NewLine, lines);
	}

	private static List<string> ReadTags(string prompt = "Tags (comma-separated, optional): ", List<string>? fallback = null)
	{
		Console.Write(prompt);
		var input = Console.ReadLine();
		if (string.IsNullOrWhiteSpace(input)) return fallback ?? new List<string>();

		return input
			.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(t => t.Trim())
			.Where(t => t.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static bool Contains(string source, string query) =>
		source?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
}
