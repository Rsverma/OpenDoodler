# The Editor Canvas

[← Back to guide index](README.md)

## Adding graphics

Drag graphics onto the canvas from the Library panel (or click **Add** on a graphic there) — see
[Library & Graphics](library-and-graphics.md). A newly added graphic is automatically capped to at
most half the board's width/height (keeping its aspect ratio), so a large SVG doesn't cover the
whole scene the moment it's added.

## Moving and resizing

- **Move**: click-drag a graphic.
- **Resize**: drag the handle at its bottom-right corner (aspect ratio is preserved).
- **Nudge**: arrow keys move the selected graphic(s) by 1px; `Shift` + arrow keys move by 10px.
- **Lock**: a locked graphic can't be moved or resized (from the canvas or the Layers panel) until
  unlocked again.

## Selecting multiple graphics

`Ctrl`-click or `Shift`-click to add graphics to the selection, or drag a selection box on empty
canvas. With multiple graphics selected you can move or delete them together, or use the Align
commands below.

## Snapping guides

While dragging a graphic, blue guide lines appear when it lines up with another graphic's edges or
center, or with the canvas edges/center — release near a guide to snap to it.

## Grouping

Select two or more graphics and choose **Group** (right-click menu) to make them move, lock, and
delete together as one unit from then on, even after the selection changes. **Ungroup** breaks
them back into independent graphics.

## Aligning

With two or more graphics selected, right-click → **Align** for: Align Left / Center / Right, and
Align Top / Middle / Bottom. A locked graphic in the selection still anchors the alignment line,
but doesn't move itself.

## Stacking order (layers)

Later items paint in front of earlier ones. Change this via:

- The **Layers panel** (Actions panel, right side) — drag the handle on any row to reorder; the
  panel lists the frontmost graphic at the top, matching the visual stacking order.
- The canvas right-click menu, or the Move buttons under the Layers panel: **Move to Top** / **Move
  Up** / **Move Down** / **Move to Bottom**.

Each row in the Layers panel also has its own lock toggle, visibility toggle, and Delay/Duration
fields (used for the stroke-by-stroke entrance animation — see
[Project Settings](project-settings.md)).

## Right-click menu

Right-clicking a graphic on the canvas gives you: Copy, Cut, Duplicate, Move to Top / Move Up /
Move Down / Move to Bottom, Group, Ungroup, Align (submenu), Locked (toggle), Hide, Delete.

## Zoom and pan

- `Ctrl` + mouse wheel zooms, anchored to the cursor position.
- Middle-mouse-button drag pans.
- Toolbar buttons above the canvas: zoom out, zoom percentage, zoom in, reset zoom (also
  re-centers), **zoom to fit** (the whole board), **zoom to selection** (whatever's currently
  selected).
