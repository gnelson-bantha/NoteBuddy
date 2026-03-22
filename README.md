# NoteBuddy

A virtual corkboard web application for managing tasks and reminders with sticky notes and pinned pictures. Organize your thoughts across multiple tabbed boards with customizable themes.

![NoteBuddy Logo](graphics/note-buddy-logo.png)

## Features

### Sticky Notes
- **Colorful Notes** — Create sticky notes in six colors: yellow, blue, green, orange, pink, and purple.
- **Titles** — Each sticky note has a title displayed in a color that matches the note.
- **Checklists** — Add up to six items per note. Check items off to mark them complete — they'll appear grayed out with a strikethrough.
- **Drag & Drop** — Drag sticky notes by their title bar to reposition them anywhere on the board.
- **Resize** — Scale notes from 1× to 2× their base size with an aspect-locked resize handle.
- **Edit & Delete** — Edit any note's title, color, or items. Delete notes from the edit dialog, the trash button, or the right-click menu.
- **Due Dates & Reminders** — Optionally set a due date with time. Choose a reminder interval (15 min, 30 min, 1 hour, or 1 day). A notification appears when the reminder window opens, and overdue dates turn red.

### Pinned Pictures
- **Upload Images** — Pin JPG, PNG, GIF, or WebP images (max 10MB) to your board with transparent backgrounds.
- **Drag Anywhere** — Click and drag pictures to reposition them. The entire image is the drag handle.
- **Right-Click Menu** — Delete pictures or adjust their z-order via the context menu.

### Tabbed Boards
- **Multiple Boards** — Organize your notes and pictures across separate tabbed boards.
- **Tab Colors** — Choose from six tab colors: yellow, blue, green, orange, pink, and purple.
- **Drag to Reorder** — Rearrange tabs by dragging them to a new position.
- **Move Items** — Right-click a sticky note or picture to move it to a different tab.
- **Edit & Delete Tabs** — Right-click a tab to edit its title/color or delete it (with confirmation).

### Organization & Filtering
- **Organize Button** — Automatically arrange all items in rows and columns, sorted alphabetically.
- **Filter** — Real-time text filter searches note titles and item text. Filters also hide non-matching tabs. Filtered notes display in a compact layout.
- **Z-Order** — Right-click any item to bring it to front, send to back, or move it forward/backward.
- **Copy as Text** — Right-click a sticky note to copy its title, items, and due date as plaintext.

### Quick Add
- **Double-Click** — Double-click on empty corkboard space to create a new note at that position.
- **Keyboard Shortcut** — Press **Alt+N** to quickly add a new note.
- **Drag & Drop Images** — Drag an image file from your desktop onto the corkboard to pin it instantly.

### Themes
- **Corkboard** — Classic cork texture (default)
- **Wood Desk** — Rich wood grain surface
- **Dark Mode** — Dark background for low-light environments
- **Graphing Paper** — Grid-lined paper
- **Bullet Journal** — Dotted journal pages
- **Watercolor Paper** — Soft textured paper

Access themes via the **Settings** button (⚙) in the header.

### Other
- **Auto-Save** — All changes are automatically saved to a local JSON file.
- **Handwritten Font** — Notes use a custom handwritten font for a natural sticky-note feel.
- **System Tray** — Runs in the Windows system tray with quick access to open or quit.

## Tech Stack

- **.NET 10** Blazor (Interactive Server)
- **MudBlazor 9.1.0** component library
- **JavaScript** interop for drag-and-drop and keyboard shortcuts
- **JSON** file persistence (no database required)
- **WiX Toolset** MSI installer

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
│   ├── Pages/Home.razor              # Main corkboard page
│   ├── Shared/
│   │   ├── StickyNoteCard.razor      # Sticky note display component
│   │   ├── StickyNoteDialog.razor    # Add/edit sticky note dialog
│   │   ├── PinnedPictureCard.razor   # Pinned picture display component
│   │   ├── PictureDialog.razor       # Upload picture dialog
│   │   ├── TabBar.razor              # Tab row component
│   │   ├── TabDialog.razor           # Add/edit tab dialog
│   │   ├── DeleteTabConfirmDialog.razor # Tab deletion confirmation
│   │   └── SettingsDialog.razor      # Theme settings dialog
│   └── Layout/MainLayout.razor       # App layout with MudBlazor providers
├── Models/
│   ├── StickyNote.cs                 # Sticky note data model
│   ├── NoteItem.cs                   # Checklist item model
│   ├── PinnedPicture.cs              # Pinned picture model
│   ├── Tab.cs                        # Tab/board model
│   ├── CorkboardData.cs              # Root data model (tabs + theme)
│   └── ZOrderAction.cs              # Z-order action enum
├── Services/
│   └── CorkboardService.cs           # JSON persistence service (tab-aware)
├── wwwroot/
│   ├── app.css                       # Global styles
│   ├── js/corkboard.js               # Drag-and-drop & keyboard interop
│   └── images/
│       ├── backgrounds/              # Theme background images
│       ├── sticky-notes/             # Sticky note color graphics
│       └── tabs/                     # Tab color graphics
├── NoteBuddy.Tray/                   # System tray launcher (WinForms)
└── NoteBuddy.Installer/             # WiX MSI installer project
```

## Data Storage

Corkboard data is saved to `%APPDATA%\NoteBuddy\corkboard.json`. This file is created automatically on first use and updated whenever you add, edit, delete, or move items. Uploaded images are stored in `%APPDATA%\NoteBuddy\uploads\`.

## License

This project is licensed under the MIT License — see [LICENSE.txt](LICENSE.txt) for details.