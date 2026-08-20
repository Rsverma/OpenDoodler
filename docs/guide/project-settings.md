# Project Settings

[← Back to guide index](README.md)

Open via the Actions panel → **Project Settings**. Applies to the whole project.

## Background Music

- **Choose File...** — pick an audio file to play underneath the whole project (mixed with any
  per-scene voiceovers on export). **✕** removes it.
- **Volume** — 0–100%.
- **Trim** — Start/End in seconds (End of `0` means "play to the file's natural end"). Loops if
  the video runs longer than the trimmed clip.

## Entrance Style

How each graphic appears when it's added to a scene:

- **Hand-drawn** — animated stroke-by-stroke, as if being drawn by a hand holding a pen.
- **Fade In** — fades in from transparent.
- **Pop In** — scales in with a slight overshoot/bounce.

## Stroke

Only shown when Entrance Style is **Hand-drawn**. Pick a pen color from the swatches, and set a
stroke width (1–20).

## Aspect Ratio

The canvas shape used by the editor, preview, and exported video: **16:9** (widescreen), **9:16**
(vertical), or **1:1** (square). Changing this rescales the editor canvas immediately.

## Scene Transitions

How the canvas changes when moving from one scene to the next:

- **None** — hard cut.
- **Crossfade** — fades between scenes.
- **Wipe** — wipes across.

Picking Crossfade or Wipe reveals a **Duration** field (0.1–5.0 seconds) for how long the
transition animation runs.
