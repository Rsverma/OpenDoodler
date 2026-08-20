# Scenes & Timeline

[← Back to guide index](README.md)

The timeline (bottom of the editor) shows every scene in your project as a card, sized
proportionally to its estimated duration (based on each graphic's Delay + Duration — see
[The Editor Canvas](editor-canvas.md)).

## Adding, selecting, and deleting scenes

- Click the **+** card at the end of the timeline to add a new blank scene after the last one.
- Click any scene card to select it and load it onto the canvas.
- Right-click a scene card → **Delete** to remove it.

## Reordering scenes

- **Drag** a scene card to any position in the timeline.
- Or right-click → **Move Left** / **Move Right** to swap it with its immediate neighbor.

## Duplicating a scene

Right-click a scene card → **Duplicate** — inserts a copy of it right after the original, with
all its graphics cloned.

## Scene transitions

Set in [Project Settings](project-settings.md): none (hard cut), crossfade, or wipe, applied
between every pair of scenes in the project, with a configurable duration in seconds.

## Voiceover (per scene)

- Right-click a scene card → **Set Voiceover...** to attach an audio file to that specific scene,
  or **Clear Voiceover** to remove it.
- A microphone icon appears on any scene card that has one, and a purple bar (with a waveform)
  appears above the scene in the timeline.
- Trim which part of the file plays: select the scene, then Actions panel → **Scene Settings**,
  and set Start/End (in seconds; End of `0` means "play to the end of the file").

## Background music (whole project)

Set from **Project Settings** — plays underneath every scene, mixed with any per-scene voiceovers
on export. Shown as a green bar (with a waveform) spanning the whole timeline, below the scene
cards. Has its own volume and start/end trim, also in Project Settings.

## The playhead

Drag the red playhead to jump between scenes — it snaps to whichever scene's card center it's
closest to when you release it.

## Timeline zoom

- `Ctrl` + mouse wheel over the timeline zooms in/out; plain wheel scrolls it horizontally.
- Toolbar buttons above the timeline: zoom out, zoom percentage, zoom in, reset zoom.
