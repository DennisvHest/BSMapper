# Bulk selection checks

Build BSMapper, then run the Godot .NET executable with:

`godot --headless --xr-mode off --path . res://tests/bulk_selection_checks.tscn`

The scene exits with code 0 on success and code 1 on a failed assertion. It checks grid/beat boundaries, reverse and zero-depth ranges, notes/bombs/wall overlap, additive ownership, shrinking/committing, object removal, deselection/deletion, and selector button signals/styles.

## Headset smoke test

1. Enter editing mode and select an object with the existing individual-selection controls.
2. Turn on **Bulk select** in the 3D ObjectTypeSelector. Confirm the placement tool highlight turns off.
3. With either controller, hold the trigger on the edit plane and drag across multiple rows and columns. Confirm cyan lane coverage, including when starting over a note or bomb; no objects should be placed or moved.
4. Release the trigger and scrub forward/backward using the existing playback controls. Confirm the cube extends from the original beat to the current beat, with matching object outlines and selection count.
5. Shrink the interval and scrub past its starting beat. Only cube-added objects outside the interval should lose selection; the original selection must remain.
6. Start another rectangle, then scrub. Objects retained from the previous cube must remain selected.
7. Toggle bulk mode off, or choose a placement tool. The cube should disappear while selected objects stay selected, and normal placement should resume.
8. While a cube is active, use Deselect All or Delete Selected. Confirm the cube disappears and subsequent scrubbing does not restore selection.
9. Switch maps or leave editing mode during a drag. Confirm there is no stale cube or drag when returning.
10. Drag beyond the grid and across another UI panel; confirm grid clamping and that clicking playback/selector controls does not start a new cube. Repeat with the other controller and check that a second simultaneous trigger does not steal the drag.
