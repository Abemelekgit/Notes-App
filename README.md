# NotesApp (.NET 8 Console)

A small JSON-backed console Notes app built with .NET 8. Supports add, list (newest first), view, search (title/body/tags), edit, delete (with confirmation), and save & exit. Notes persist to `notes.json` alongside the executable.

## Prerequisites
- .NET SDK 8.x (`dotnet --version` should show 8.*)
- Terminal and VS Code (Copilot recommended)

## Quick start
1) Install .NET 8 (Ubuntu 24.04):
   - `wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb`
   - `sudo dpkg -i /tmp/packages-microsoft-prod.deb`
   - `sudo apt-get update`
   - `sudo apt-get install -y dotnet-sdk-8.0`
   - Verify: `dotnet --version` (should print 8.x)
2) Restore/build:
   - `dotnet build`
3) Run the app:
   - `dotnet run`

## How to use
- The menu shows options 1-7. Enter the number to run that action.
- Add note: enter title (required), body (finish with an empty line), and optional tags (comma-separated). Saves immediately.
- List notes: displays notes sorted by last updated (newest first) with numbers you can use to view/edit/delete.
- View note: choose a note number; shows metadata and full body.
- Search: finds matches in title/body/tags (case-insensitive) and lists results newest-first.
- Edit: select note number, press Enter to keep existing values, update tags with comma-separated input.
- Delete: select note number, then type `yes` to confirm.
- Save & Exit: writes `notes.json` and quits (changes also save automatically after edits/add/delete).

## Data
- Stored at `notes.json` in the app directory. Safe to back up or sync.

## Recreating this with Copilot (step-by-step)
1) `dotnet new console --framework net8.0` in an empty folder.
2) Open `Program.cs` and ask Copilot to scaffold a simple menu-based notes app with JSON persistence.
3) Define a `Note` record with Id, Title, Body, Tags, CreatedAt, UpdatedAt.
4) Add load/save helpers using `System.Text.Json` pointing at `notes.json` under `AppContext.BaseDirectory`.
5) Implement menu actions: Add, List (sorted by UpdatedAt desc), View (by number), Search (title/body/tags), Edit (keep values on Enter), Delete (confirm with `yes`), Save & Exit.
6) Add helper prompts for required fields, optional multiline body, and comma-separated tags.
7) Build and run: `dotnet build` then `dotnet run`.

## Web UI (Razor Pages)
- Restore/build: `dotnet build NotesApp.Web`
- Run locally: `dotnet run --project NotesApp.Web`
- Open the UI: https://localhost:5001 (or the HTTP port shown in the console)
- Data lives in `NotesApp.Web/notes.json` by default; it is created automatically.
- A `.bak` copy of `notes.json` is written before saves for safety.

## API endpoints
- `GET /api/notes?query={q}` — list notes, optional search
- `GET /api/notes/{id}` — fetch single note
- `POST /api/notes` — create (JSON: { title, body, tags: [] })
- `PUT /api/notes/{id}` — update
- `DELETE /api/notes/{id}` — remove
- `GET /health` — basic health check

Validation
- Title is required and max 200 characters
- Body max 4000 characters

## Notes
- Time stamps use UTC format (`u`) for predictable sorting and display.
- Tags are deduplicated case-insensitively when entered.

## Changelog
- 2026-03-10: Small documentation refresh commit.
- 2026-03-10: fast contribution 12:02:28 #1
- 2026-03-10: fast contribution 12:02:30 #2
