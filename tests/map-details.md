# Map detail saving

Build the project, then run the integration scene:

```powershell
dotnet build --no-restore
& 'C:/Program Files/Godot/Godot_v4.7.2-stable_mono_win64_console.exe' --headless --xr-mode off --path . res://tests/map_details_checks.tscn
```

The scene creates an isolated fixture under `tests`, cleans up its files, and exits with code 0 on success or 1 on assertion failure. Its 37 checks cover:

- Clean form state, metadata changes, Enter/focus exit without saving, and reverting changes.
- Unchecking an existing difficulty warns about deletion while preserving its file; rechecking cancels the deletion. The warning must be fully on screen, outside any clipped scroll area, without scrolling.
- Typing difficulty values shows Save and changes only persist when saved, preserving difficulty objects.
- Invalid timing input keeps Save visible, reports an error, and leaves files unchanged.
- Audio and cover changes remain pending until Save.
- Saving updates metadata, audio and cover, hides Save, and keeps the form visible.
- Saving a removed difficulty deletes its file and metadata and clears the warning.
- New difficulties have no deletion warning; loading a map resets pending changes; new maps retain Create map.
- Clean maps open without a prompt. Unsaved edits block opening VR until Save and continue or Discard is chosen; Cancel keeps the edits. Invalid values prevent saving and navigating.
- Switching maps waits for the same dialog. Saving updates the previous map before switching; discarding preserves its saved data. Reselecting the same map after saving shows fresh data.
- Starting a new map warns about existing changes. Unfinished new maps are also protected; incomplete drafts cannot be saved and left. Difficulty changes also trigger the dialog, including the pending deletion warning.

The deletion warning sits above the action buttons, outside the scrollable form, so it stays visible while scrolling through difficulties.

Validation: the visibility regression fails with the warning inside the scrollable form and passes after moving it above the action buttons. All 37 integration checks passed, and `dotnet build --no-restore` succeeded (one existing `IconButton.UpdateMinimumSize` warning). Headless checks log existing SVG cover-loading warnings. The VR navigation tests observe the editor-open request without starting XR.

For a desktop smoke test, edit several fields and replace the audio/cover, then verify that Save appears and nothing is persisted before clicking it. Uncheck a difficulty and verify the warning is readable and names it; recheck it to cancel deletion. Save changes and reopen the map to confirm they persisted.
