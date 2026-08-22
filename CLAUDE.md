# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

OpenDoodler (assembly/project name `OpenBoardAnim`) is an open-source whiteboard/doodle animation desktop app —
a WPF (.NET 10) app that lets users compose SVG/text graphics on a canvas, animate them stroke-by-stroke, and
export the result as video.

## Commands

```bash
# Build the whole solution
dotnet build OpenBoardAnim.sln -c Debug

# Run the app (WPF, Windows-only — must run on Windows, not WSL/Linux)
dotnet run --project src/OpenBoardAnim/OpenBoardAnim.csproj
# or launch the built exe directly:
src/OpenBoardAnim/bin/Debug/net10.0-windows/OpenBoardAnim.exe

# EF Core migrations (run from src/OpenBoardAnim.Library/)
dotnet ef migrations add <Name> --project src/OpenBoardAnim.Library
dotnet ef database update --project src/OpenBoardAnim.Library

# Run the xUnit test suite
dotnet test tests/OpenBoardAnim.Tests/OpenBoardAnim.Tests.csproj
```

There is no lint config — do not invent lint commands.

## Repository layout

Buildable source lives under `src/` (three projects, listed below); the xUnit test project lives under
a sibling `tests/` directory instead, since it isn't itself app source. The solution file
(`OpenBoardAnim.sln`) stays at the repo root and references all four. `docs/images/` holds README-only
screenshots/GIFs — not app resources, don't confuse with `src/OpenBoardAnim/Resources/` (real
compiled-in app assets like `App.ico`, `pencil.png`, the `peep-*.svg` characters). `installer/` holds a
separate WiX Toolset project/solution (`OpenBoardAnim.Setup.sln`) that builds an MSI from a
`dotnet publish` of `OpenBoardAnim` — deliberately not part of `OpenBoardAnim.sln`, see
`installer/README.md`.

## Solution structure

Four projects in `OpenBoardAnim.sln`:

- **`src/OpenBoardAnim`** — the WPF UI app (`net10.0-windows`, `UseWPF=true`). Uses HandyControl and
  MaterialDesignThemes for UI, SharpVectors for SVG rendering, `Microsoft.Extensions.DependencyInjection` for DI.
- **`src/OpenBoardAnim.Library`** — data layer: EF Core + SQLite (`DataContext`, `Entities/`, `Migrations/`,
  `Repositories/`).
- **`src/OpenBoardAnim.Utilities`** — cross-cutting helpers: Serilog-based logging (`LogWriter.cs`) and
  `EnumHelper.cs`. No project reference back to the WPF app or Library.
- **`tests/OpenBoardAnim.Tests`** — xUnit + Moq unit tests (`net10.0-windows`, `UseWPF=true` since it
  exercises ViewModels/services that reference WPF types like `ICommand`/`MessageBoxResult`). Project
  references all three `src/` projects. Covers pure logic (`ExportProgressMath`), services against
  mocked repositories (`CacheService`), and ViewModels with interface-only dependencies
  (`EditorActionsViewModel`, `EditorCanvasViewModel`, `EditorTimelineViewModel`, `StateSnapshotService`).
  Deliberately does not cover the WPF rendering pipeline (`GeometryHelper`, `PathAnimationHelper`,
  `PreviewAndExportHandler`'s animation methods) or anything requiring a live `Application.Current`
  (`ThemeService`'s `ApplySkin`/`CurrentTheme`) — not economically unit-testable without a much larger
  rendering-abstraction rearchitecture.

## Architecture

### Dependency injection

Configured manually in `src/OpenBoardAnim/App.xaml.cs` in the `App` constructor, using a plain
`Microsoft.Extensions.DependencyInjection` `ServiceCollection` (no host builder). It registers `DataContext`, the
four repositories (`ShapeRepository`, `GraphicRepository`, `SceneRepository`, `ProjectRepository`) as singletons,
`INavigationService`/`IPubSubService`/`IDialogService` and their implementations, `CacheService`,
`StateSnapshotService`, all ViewModels (singleton, except `LaunchViewModel` which is transient), and a
`Func<Type, ViewModel>` factory delegate used by `NavigationService` to resolve views by type. `OnStartup`
resolves `MainWindow` from the container and shows it. Both the constructor and `OnStartup` wrap setup in
try/catch routed to `Logger.LogError` (see Logging below) — mirror that pattern for any new startup-critical code.

### MVVM

Manual MVVM, no CommunityToolkit.Mvvm or other messenger library:

- `src/OpenBoardAnim/Core/ObservableObject.cs` implements `INotifyPropertyChanged`; `ViewModel` (in `Core/`) is an
  abstract base all ViewModels derive from; `RelayCommand` (in `Core/`) implements `ICommand`.
- ViewModels live in `src/OpenBoardAnim/ViewModels/` (`MainViewModel`, `LaunchViewModel`, `EditorViewModel`,
  `EditorActionsViewModel`, `EditorCanvasViewModel`, `EditorLibraryViewModel`, `EditorTimelineViewModel`), with
  matching Views in `src/OpenBoardAnim/Views/`.
- Cross-ViewModel communication goes through a manual pub/sub mediator: `IPubSubService`/`PubSubService`
  (`src/OpenBoardAnim/Services/PubSubService.cs`) with a `SubTopic` enum (`SceneReplaced`, `SceneChanged`,
  `GraphicAdded`, `ProjectLaunched`, `ProjectExporting`). Prefer this over adding direct ViewModel-to-ViewModel
  references.
- Navigation between views goes through `INavigationService`/`NavigationService`, which resolves views via the
  DI-registered `Func<Type, ViewModel>` factory rather than `new`-ing them up directly.

### Persistence

EF Core over SQLite. `src/OpenBoardAnim.Library/DataContext.cs` (`DataContext : DbContext`) exposes
`DbSet<GraphicEntity> Graphics`, `DbSet<ProjectEntity> Projects`, and `DbSet<SceneTemplateEntity>
SceneTemplates`. The DB file lives at `%LocalAppData%\OpenBoardAnim.db`; `App.xaml.cs` runs
`Database.Migrate()` on startup. Entities: `GraphicEntity`, `ProjectEntity`, `SceneTemplateEntity` (the
scene-template gallery — each row stores a full serialized `SceneModel` as JSON, self-contained rather
than referencing `GraphicEntity` rows, seeded with a handful of built-in starter layouts on first run;
user-saved ones come from "Save Current Scene as Template" in the library panel). Repositories
(`GraphicRepository`, `ProjectRepository`, `SceneRepository`, `ShapeRepository`) wrap `DataContext` and
are the only things ViewModels should talk to for persistence — don't reach into `DataContext` directly
from a ViewModel.

### Undo/redo

`src/OpenBoardAnim/Services/StateSnapshotService.cs` implements the memento pattern with two `Stack<ProjectDetails>`
fields (`undoStack`/`redoStack`). `ProjectDetails` (`src/OpenBoardAnim/Models/ProjectDetails.cs`) is the whole-project
snapshot object. `SaveState` pushes a snapshot (no-ops if identical to the last one) and clears the redo stack;
`Undo`/`Redo` pop from one stack and push onto the other. It's a DI singleton — inject it rather than constructing
a new one.

### Export / rendering pipeline

- `src/OpenBoardAnim/Utils/PreviewAndExportHandler.cs` — static `RunAnimationsOnCanvas(ProjectDetails, Canvas,
  isExport)` replays a project's scenes onto a WPF `Canvas`, animating strokes via `PathAnimationHelper` and
  geometry conversion via `GeometryHelper`. This is the shared code path for both live preview and export
  (`isExport` toggles the recording side).
- `src/OpenBoardAnim/Utils/VideoExporter.cs` — when exporting, hooks `CompositionTarget.Rendering` to capture
  `RenderTargetBitmap` frames of the canvas as PNGs under `%TEMP%\WpfAnimationFrames`, then shells out to
  `DLLs\ffmpeg.exe` (bundled in the app's output dir, copied via `.csproj`) through `Process`/`ProcessStartInfo`
  to encode the frames into an MP4.
- Export runs in the background off the UI thread (see recent commit "Updated the export feature to render in
  background") — preserve that when touching the export path so the UI doesn't block during long renders.

### Logging

`src/OpenBoardAnim.Utilities/LogWriter.cs`, static class `Logger`. Serilog with three severity-filtered file sinks
(Information → `Logs\Messages.log`, Warning → `Logs\Warnings.log`, Error → `Logs\Exceptions.log`), rolling daily
and at 3MB, under `AppDomain.CurrentDomain.BaseDirectory`. Use `Logger.LogError(ex, LogAction)`,
`Logger.LogWarning(msg, LogAction)`, `Logger.LogMessage(msg, LogAction)` rather than writing new logging code;
`LogAction` (`LogOnly`, `LogAndShow`, `LogAndThrow`) controls whether the error also surfaces to the UI. This is
used pervasively via try/catch wrappers throughout the app (`App.xaml.cs`, `CacheService`, `PubSubService`,
`VideoExporter`, `PreviewAndExportHandler`) — follow the same try/catch-and-log convention for new
startup/IO/export code.
