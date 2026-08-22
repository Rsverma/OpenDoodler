<h1 align="center">
  <br>
  <img src="https://raw.githubusercontent.com/Rsverma/OpenDoodler/main/src/OpenBoardAnim/Resources/App.ico" alt="Open Doodler" width="200">
  <br>
  Open Doodler
  <br>
</h1>

<h4 align="center">An open source animation software for White board animation.</h4>

<p align="center">
  <a href="https://github.com/Rsverma/OpenDoodler/actions/workflows/ci.yml"><img src="https://github.com/Rsverma/OpenDoodler/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-AGPL--3.0-blue.svg" alt="License: AGPL-3.0"></a>
  <a href="https://github.com/Rsverma/OpenDoodler/releases"><img src="https://img.shields.io/github/v/release/Rsverma/OpenDoodler" alt="Latest release"></a>
</p>

<p align="center">Windows only — built with WPF (.NET 10).</p>

<p align="center">
  <a href="#key-features">Key Features</a> •
  <a href="#wishlist">Wishlist</a> •
  <a href="#how-to-use">How To Use</a> •
  <a href="#building-from-source">Building From Source</a> •
  <a href="#credits">Credits</a> •
  <a href="#related">Related</a>
</p>

![](https://github.com/Rsverma/OpenDoodler/blob/main/docs/images/Animation.gif)

## Key Features

* Launch page to create a new project or pick up a recent one - shown as a thumbnail of its first scene - with unsaved-changes protection and automatic crash recovery from a periodic autosave backup
* Editor canvas to compose SVG and text graphics, with drag, resize, zoom/pan (plus zoom-to-fit and zoom-to-selection), snapping/alignment guides while dragging, lock-in-place, persistent grouping, a visual layers panel with visibility/lock toggles and drag-to-reorder, multi-select with group move/delete/align, a right-click context menu (copy/cut/duplicate/lock/group/align/move to top/up/down/bottom/delete), and copy/paste across scenes
* Library Manager to import graphics, delete graphics you no longer need, and clean up any invalid/corrupted graphics from your library
* Scene-template gallery: built-in starter layouts plus your own saved scenes, inserted as a brand-new scene so picking one never overwrites your current work
* Stroke-by-stroke hand-drawn animation with configurable stroke color/width, plus Fade In / Pop In as alternate entrance styles
* Whole-project timeline: scenes sized proportionally to their duration with a draggable, zoomable playhead, drag-to-reorder/duplicate/delete, hard-cut/crossfade/wipe transitions with configurable duration, per-scene voiceover (with trim in/out points and waveform display) layered under project-wide background music, and the ability to preview a single scene in isolation
* Undo/redo
* Light / Dark / System UI theme
* Project Settings: board type, background music, stroke styling, entrance style, aspect ratio (16:9 / 9:16 / 1:1), and scene transitions
* In-app preview and MP4 video export (background music and per-scene voiceovers mixed in) with progress reporting and cancellation
* Keyboard shortcuts for undo/redo, save, new/open project, delete, cut/copy/paste, and nudging the selected graphic(s)

## Wishlist
* Configurable hand/pen (choose a hand skin, pen color, or turn the hand off entirely)
* Camera pan/zoom (Ken Burns-style focus moves)
* Animated GIF export alongside MP4
* Custom/arbitrary export resolution beyond the built-in aspect-ratio presets

## How To Use

See [Installing OpenDoodler](docs/installing.md) for the `.msi` installer, and the
[User Guide](docs/guide/README.md) for how to use the editor once it's installed.

## Building From Source

To clone and run this application, you'll need [Git](https://git-scm.com) and [Visual Studio](https://visualstudio.microsoft.com/). From your command line:

```bash
# Clone this repository
$ git clone https://github.com/Rsverma/OpenDoodler.git

# Go into the repository

# Open Solution File OpenBoardAnim.sln in Visual Studio
# Set OpenBoardAnim as startup project
# Launch the application to get started
```

## Contributing

Contributions are welcome — feel free to open an issue for bugs/ideas or submit a pull request.

## Credits

This software uses the following open source packages:

- [HandyControl](https://github.com/HandyOrg/HandyControl) - UI controls and theming
- [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) - UI controls and theming
- [SharpVectors](https://github.com/ElinamLLC/SharpVectors) - SVG rendering
- [NAudio](https://github.com/naudio/NAudio) - audio playback and waveform display
- [Serilog](https://github.com/serilog/serilog) - logging
- [Entity Framework Core](https://github.com/dotnet/efcore) (SQLite provider) - local project database
- [FFmpeg](https://ffmpeg.org/) - bundled for MP4 video export

## Related

- [RSV Asset Manager](https://github.com/Rsverma/RSVAssetManager) - A minimal Asset Manager application, also built by RSV Enterprise Solutions.

---
<a href="mailto:rsverma333@gmail.com"><img src="https://img.shields.io/badge/gmail-%23DD0031.svg?&style=for-the-badge&logo=gmail&logoColor=white"/></a>
> GitHub [@Rsverma](https://github.com/Rsverma) &nbsp;&middot;&nbsp;
> Youtube [@Code With RSV](https://www.youtube.com/channel/UCHXfV0ENFtcM-rBEe3FyvAg) &nbsp;&middot;&nbsp;
> LinkedIn [@rsverma333](https://www.linkedin.com/in/rsverma333/)
