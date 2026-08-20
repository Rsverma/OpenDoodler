# Getting Started

[← Back to guide index](README.md)

## The Launch screen

When you open OpenDoodler, you land on the Launch screen:

- **Recent Projects** — a card per project, each showing a thumbnail of its first scene, the
  project name, scene count, and creation date. Click **Edit** to open it, or **Delete** to
  remove it (this also removes its thumbnail and its recent-projects entry — your `.obap` project
  file on disk isn't touched).
- **Create New Project** — starts a brand-new project with a single blank scene.

If OpenDoodler finds an autosaved backup from a previous session that was never explicitly saved
(e.g. after a crash), it'll ask if you want to recover it the next time you launch.

## Creating vs. opening a project

- **New Project** (Launch screen button, or File → New Project / `Ctrl+N`) starts fresh with one
  blank scene at the default aspect ratio.
- **Open Project** (File → Open Project / `Ctrl+O`) opens an existing `.obap` project file from
  disk.

If you have unsaved changes when you start a new project, open another one, or close the app,
OpenDoodler asks whether to save first.

## Saving

- `Ctrl+S`, or the **Save** button in the Actions panel.
- The first save prompts for a file location; every save after that just writes to the same file.
- The title bar shows the current project's name, with a `*` prefix while there are unsaved
  changes.

## Autosave and crash recovery

While a project is open, OpenDoodler periodically writes a backup to disk in the background (this
only actually writes when something has changed — an unmodified project doesn't get re-saved
every cycle). If the app closes without an explicit save (a crash, or closing without confirming),
that backup is offered for recovery the next time you launch.

## The editor layout

Once a project is open, the editor is laid out as:

- **Library** (left, collapsible) — graphics, shapes, text, and scene templates. See
  [Library & Graphics](library-and-graphics.md).
- **Canvas** (center) — where you compose and arrange the current scene. See
  [The Editor Canvas](editor-canvas.md).
- **Actions** (right, collapsible) — Save/Preview/Export/Close, Scene/Project Settings, and the
  Layers panel for the current scene.
- **Timeline** (bottom) — every scene in the project, plus voiceover and background-music tracks.
  See [Scenes & Timeline](scenes-and-timeline.md).
