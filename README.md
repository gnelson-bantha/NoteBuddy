# NoteBuddy

A virtual corkboard web application for managing tasks and reminders with sticky notes and pinned pictures.

![NoteBuddy Logo](graphics/note-buddy-logo.png)

## Features

- **Sticky Notes** — Create colorful sticky notes with checklist items. Choose from six colors: yellow, blue, green, orange, pink, and purple.
- **Titles** — Each sticky note has a title displayed in a color that matches the note.
- **Checklists** — Add up to six items per note. Check items off to mark them complete — they'll appear grayed out with a strikethrough.
- **Drag & Drop** — Click the move icon to drag sticky notes and pictures anywhere on the corkboard.
- **Edit & Delete** — Edit any note's title, color, or items. Delete notes and pictures when you're done with them.
- **Pinned Pictures** — Upload images to pin on your corkboard. Supports JPG, PNG, GIF, and WebP (max 10MB, displayed at up to 200×200px).
- **Auto-Save** — All changes are automatically saved to a local JSON file. Your corkboard persists across app restarts.
- **Handwritten Style** — Notes use a custom handwritten font for a natural sticky-note feel.
- **Cork Background** — A seamless tiled cork texture gives the board a realistic look.

## Tech Stack

- **.NET 10** Blazor (Interactive Server)
- **MudBlazor 9.1.0** component library
- **JavaScript** interop for drag-and-drop
- **JSON** file persistence (no database required)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Getting Started

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd NoteBuddy
   ```

2. Run the application:
   ```bash
   cd NoteBuddy
   dotnet run
   ```

3. Open your browser to **http://localhost:5150**

## Project Structure

```
NoteBuddy/
├── Components/
│   ├── Pages/Home.razor           # Main corkboard page
│   ├── Shared/
│   │   ├── StickyNoteCard.razor   # Sticky note display component
│   │   ├── StickyNoteDialog.razor # Add/edit sticky note dialog
│   │   ├── PinnedPictureCard.razor# Pinned picture display component
│   │   └── PictureDialog.razor    # Upload picture dialog
│   └── Layout/MainLayout.razor    # App layout with MudBlazor providers
├── Models/
│   ├── StickyNote.cs              # Sticky note data model
│   ├── NoteItem.cs                # Checklist item model
│   ├── PinnedPicture.cs           # Pinned picture model
│   └── CorkboardData.cs           # Root data model
├── Services/
│   └── CorkboardService.cs        # JSON persistence service
├── wwwroot/
│   ├── css/app.css                # Global styles
│   ├── js/corkboard.js            # Drag-and-drop JavaScript interop
│   ├── images/                    # Sticky note graphics, cork background, fonts
│   └── uploads/                   # User-uploaded pictures
└── Data/
    └── corkboard.json             # Auto-generated data file
```

## Data Storage

Corkboard data is saved to `NoteBuddy/Data/corkboard.json`. This file is created automatically on first use and updated whenever you add, edit, delete, or move a sticky note or picture. Uploaded images are stored in `NoteBuddy/wwwroot/uploads/`.

## License

This project is licensed under the MIT License — see [LICENSE.txt](LICENSE.txt) for details.