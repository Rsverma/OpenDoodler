# Library & Graphics

[← Back to guide index](README.md)

The Library panel (left side of the editor) has five tabs: **Scenes**, **Graphics**, **Shapes**,
**Text**, and **Audio**. (Audio, and one further unlabeled tab, are placeholders — not
implemented yet.)

## Graphics tab

- **Search** — type in the search box and click the search icon (or leave it blank and search to
  show everything) to filter your library.
- **Load More** — loads the next page of results.
- **Add** — click **Add** on any graphic's card to drop a copy of it into the current scene.
- **Manage Library** — opens the Library Manager (see below).

## Library Manager

A dedicated window for maintaining your graphics library, separate from just browsing to add
graphics to a scene:

- **Import Graphics** — add your own SVG files to the library.
- **Delete** — remove a graphic you no longer want from the library. This only removes the
  library entry — copies already placed on a scene in any project are untouched (graphics are
  copied, not referenced, when you add them to a scene).
- **Cleanup** (the broom icon) — scans your *entire* library (not just what's currently loaded)
  and removes any graphic that fails to load (corrupted or empty SVG data). Reports how many it
  removed.

## Shapes tab

A read-only catalog of built-in shapes — same **Add** behavior as Graphics, but nothing here can
be imported, deleted, or searched.

## Text tab

Add a text graphic to the current scene:

1. Type your text.
2. Pick a font family and style from the dropdowns.
3. Pick a color from the swatches.
4. Set a font size and, optionally, underline.
5. Click **Add Text**.

The preview at the bottom of the tab shows what your current settings will look like.

## Scenes tab (template gallery)

A gallery of starter scene layouts — a mix of built-in templates and any you've saved yourself:

- **Use** — inserts the template as a brand-new scene right after your currently selected one.
  This never overwrites your current scene.
- **Save Current Scene as Template** — saves whatever's on the currently selected scene as a
  reusable template of your own (name it when prompted). Only your own saved templates can be
  deleted from the gallery; built-in ones can't.
