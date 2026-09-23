# Selection copy/paste checks

Build BSMapper, then run the Godot .NET executable from the repository root:

`godot --headless --xr-mode off --path . res://tests/copy_paste_checks.tscn`

Also run the existing regression scene:

`godot --headless --xr-mode off --path . res://tests/bulk_selection_checks.tscn`

The copy/paste checks cover independent note/bomb/wall snapshots, nested metadata, earliest-beat anchoring, relative offsets, repeat paste, source edits, wall save/reload, button events/availability, and the real editor scene's clipboard actions, copied outlines, source deletion, and map-change cleanup. The scene exits with code 0 on success and 1 on assertion failure. Headless rendering does not verify VR appearance.

## Validation

- Project build succeeded.
- All 38 copy/paste checks and all 30 existing bulk-selection checks passed; both scenes exited with code 0.
- The full-editor headless run logs existing invalid-UID warnings that fall back to valid resource paths, plus two `Viewport Texture must be set to use it` diagnostics. Loading the unchanged XR Tools `viewport_2d_in_3d.tscn` resource alone reproduces both viewport diagnostics, independently of clipboard code.
- Headset rendering and controller interaction remain manual checks.

## Headset smoke test

1. Select notes, bombs, and walls, using either individual selection or a bulk cube. Press **Copy** on the in-world SelectionPanel. Every copied source should have a green outline instead of blue. Other selected/hovered objects should not change color.
2. Scrub forward and press **Paste**. The earliest copied object's beat should match the current playback position; all other objects should keep their relative beat spacing, grid positions, note colors/directions, and wall sizes/durations. Original objects must remain in place.
3. Scrub backward and paste again. The same clipboard should be reused without modifying previous copies. Pasted objects should initially be unselected.
4. Deselect the originals. Their green clipboard markers and the panel's Paste button should remain available. Copy, Deselect, and Delete should be disabled when there is no selection.
5. Edit or delete a source after copying, then paste. The captured snapshot should remain unchanged and usable.
6. Copy a different selection. The previous source markers should revert to their ordinary highlight state, and only the new clipboard's sources should be green.
7. Change maps. The clipboard should clear, old markers should disappear, and Paste should be unavailable until a new Copy.
8. Save/reload a map containing pasted notes, bombs, and walls. Confirm beat positions, attributes, and dimensions are preserved.
9. Check that the enlarged panel and both buttons are legible and reachable with either controller, and that clicking them does not place notes or start a new selection cube.
