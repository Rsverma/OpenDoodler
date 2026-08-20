# Preview & Export

[← Back to guide index](README.md)

## Preview

- **Actions panel → Preview** plays back the whole project, from scene 1, with background music
  and voiceovers.
- **Right-click a scene card → Preview Scene** instead plays just that one scene in isolation
  (music/voiceover are still correctly time-offset as if the rest of the project played first).

## Export to MP4

**Actions panel → Export**, then choose where to save the `.mp4` file. Export re-plays the whole
project like Preview does, but also records it to video and mixes in background music and every
scene's voiceover.

A progress bar and status text appear while exporting, in three phases:

1. **Capturing** — recording each animation frame (0–70%).
2. **Saving** — flushing any not-yet-written frames to disk (70–80%).
3. **Encoding** — ffmpeg encoding the captured frames into the final MP4 (80–100%).

Click **Cancel Export** at any point to stop early.

## Notes

- Export resolution follows the project's Aspect Ratio setting (see
  [Project Settings](project-settings.md)) — always exactly 2× the editor canvas's own size.
- Export runs in the background, so the editor stays usable while it's in progress.
