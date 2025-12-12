using System.Text.Json;
using NotesApp.Web.Services;
using NotesApp.Web.Models;

var builder = WebApplication.CreateBuilder(args);

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
    if (string.IsNullOrWhiteSpace(input.Title))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(input.Title)] = new[] { "Title is required." }
        });
    }

    var created = await store.AddAsync(input);
    return Results.Created($"/api/notes/{created.Id}", created);
});

app.MapPut("/api/notes/{id:guid}", async (INoteStore store, Guid id, NoteInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.Title))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(input.Title)] = new[] { "Title is required." }
        });
    }

    var updated = await store.UpdateAsync(id, input);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.MapDelete("/api/notes/{id:guid}", async (INoteStore store, Guid id) =>
{
    var deleted = await store.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();
