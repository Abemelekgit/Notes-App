using System.Text.Json;
using NotesApp.Web.Services;
using NotesApp.Web.Models;

var builder = WebApplication.CreateBuilder(args);

const int MaxTitleLength = 200;
const int MaxBodyLength = 4000;

builder.Services.AddRazorPages();
builder.Services.AddSingleton<INoteStore>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var filePath = Path.Combine(env.ContentRootPath, "notes.json");
    return new JsonNoteStore(filePath);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/notes", async (INoteStore store, string? query) =>
{
    var notes = await store.SearchAsync(query);
    return Results.Ok(notes.OrderByDescending(n => n.UpdatedAt));
});

app.MapGet("/api/notes/{id:guid}", async (INoteStore store, Guid id) =>
{
    var note = await store.GetAsync(id);
    return note is null ? Results.NotFound() : Results.Ok(note);
});

app.MapPost("/api/notes", async (INoteStore store, NoteInput input) =>
{
    if (!Validate(input, out var errors)) return Results.ValidationProblem(errors);

    var created = await store.AddAsync(input);
    return Results.Created($"/api/notes/{created.Id}", created);
});

app.MapPut("/api/notes/{id:guid}", async (INoteStore store, Guid id, NoteInput input) =>
{
    if (!Validate(input, out var errors)) return Results.ValidationProblem(errors);

    var updated = await store.UpdateAsync(id, input);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.MapDelete("/api/notes/{id:guid}", async (INoteStore store, Guid id) =>
{
    var deleted = await store.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();

bool Validate(NoteInput input, out Dictionary<string, string[]> errors)
{
    errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(input.Title))
    {
        errors[nameof(input.Title)] = new[] { "Title is required." };
    }
    else if (input.Title.Length > MaxTitleLength)
    {
        errors[nameof(input.Title)] = new[] { $"Title must be at most {MaxTitleLength} characters." };
    }

    if (input.Body?.Length > MaxBodyLength)
    {
        errors[nameof(input.Body)] = new[] { $"Body must be at most {MaxBodyLength} characters." };
    }

    return errors.Count == 0;
}
