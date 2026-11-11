# BSMapper application overview

BSMapper is a room-scale Beat Saber map editor built with Godot and C#. It presents a beatmap as objects in a 3D lane and uses OpenXR controllers for playback, selection, placement, and editing. The desktop application is primarily a launcher and map-management interface; the main editing experience takes place in VR.

## What the application does

Users point BSMapper at a Beat Saber installation, browse maps in its `CustomWIPLevels` folder, and open a map difficulty in the editor. In the headset, notes, bombs, and walls move through a lane in time with the song. Users can switch between playback and editing, place new notes or bombs on a 4-by-3 grid, move objects, select and adjust them, use bulk selection and clipboard operations, and save changes to the selected difficulty. A beat grid and optional song spectrogram help line up objects with the music.

The start screen also supports creating maps and configuring their metadata, audio, cover image, and Standard difficulties. Changes to existing map details remain pending until the Save button is clicked. Switching maps, starting a new map, or opening the VR editor with unsaved details prompts the user to save and continue, discard changes, or cancel. Unfinished new maps are also protected when navigating away. Unchecking an existing difficulty shows a warning that saving will permanently delete the difficulty and all its objects. Map metadata and difficulty files are written in Beat Saber v2 JSON format.

## Main user flow

1. **Configure the game folder.** On launch, BSMapper loads the saved install path from `user://settings.cfg`. The selected folder is accepted when it contains `Beat Saber_Data`; if `Beat Saber_Data/CustomWIPLevels` is missing, BSMapper creates it.
2. **Choose or create a map.** The start screen lists WIP maps and presents map metadata and difficulty information. New maps and their audio are created under the WIP maps directory.
3. **Choose a difficulty.** If a map has several difficulties, the user selects one before entering the editor.
4. **Edit in VR.** BSMapper initializes OpenXR, enables XR rendering, and switches to the editor scene. If OpenXR is unavailable, it prompts the user to connect a headset. A selected difficulty can be retained across the requested restart into VR.
5. **Save the difficulty.** While the VR editor is open, the selected `.dat` difficulty file is autosaved every 30 seconds during both playback and editing. A small world-space panel above the object-type selector shows the latest successful autosave time and a Save map button. The button and existing controller save action save immediately and report success or failure in the panel. Failed autosaves retain the previous successful timestamp and retry on the next interval. Opening a different difficulty resets the timer and timestamp.

## Editing model and controls

The lane uses Beat Saber coordinates: four horizontal columns and three vertical layers. Beat timing is stored in beats; the lane converts beats to seconds using the difficulty BPM and uses note jump speed (NJS) and reaction-time settings to position and animate objects. In editing mode, objects are shown in a stable editing view and a controller pointer interacts with the grid. In playback mode, the saber simulation and timed object motion are active.

The current controller bindings are defined in `editor/InputManager.cs` and interpreted by `editor/Editor.cs`:

- Left controller primary button is mapped to the playback/edit toggle and the editor's delete action; the right controller primary button saves unless it deletes a pointed object. Controller primary-click steps the beat subdivision backward on the left and forward on the right.
- Left stick scrubs playback. Releasing it snaps the playhead to the nearest subdivision.
- Trigger interacts with the edit grid and selects objects when selection mode is active. Grip starts object dragging or toggles selection mode, depending on hand and mode.

The editor also exposes selection tools for changing note type and cut direction, moving selected objects by beat subdivision, bulk selection, and copy/cut/paste. Paste preserves the objects' relative beat offsets and places the first copied object at the current playback beat. Walls are represented and loaded/saved, though the edit-plane drag operation is limited to notes and bombs.

An in-world note jump settings panel below the selection tools is available in editing mode. Its minus/plus buttons adjust NJS by 0.1 m/s and start beat offset by 0.01 beats, including negative offsets. Select either number to open a numeric keypad to the right, using Godot XR Tools' existing keyboard event handling. The keypad includes digits, minus, decimal point, backspace, and Enter; a physical keyboard also works. Enter or leaving the field applies the value, and invalid input restores the previous value. Editing uses a fixed 10 m/s timeline scale for objects, walls, selection bounds, beat markings, and the spectrogram. NJS and jump offset affect playback only. The editor's Save and autosave actions also write these settings to `info.dat` for the current difficulty.

## Technical structure

The project is a Godot 4.7 C# application targeting .NET 8. `project.godot` enables OpenXR and registers shared autoloads. Scenes compose the desktop start interface and VR editor; C# scripts implement application logic, and `.tscn` scenes define the XR rig, lane, controls, and object visuals.

| Area | Responsibility |
| --- | --- |
| `Main.cs`, `main.tscn`, `ui/` | Install selection, map browsing, metadata editing, difficulty selection, and transition to the editor. |
| `beat_saber/maps/` | Beat Saber metadata/difficulty models, v2 JSON parsing and serialization, map file operations, and the shared `BeatMapManager`. |
| `beat_saber/` | Runtime scene objects for notes, bombs, walls, sabers, and the lane that maps beatmap data to 3D objects. |
| `editor/Editor.cs`, `editor/InputManager.cs` | XR controller input, object interaction, selection, clipboard, and editing-mode behavior. |
| `editor/PlaybackManager.cs` | Audio playback, playhead, beat/subdivision snapping, scrubbing, and playback/editing mode state. |
| `editor/ObjectEditPlane/` | Grid placement, beat grid, selection tools, playback progress, and spectrogram display. |
| `addons/` | Godot XR tools and supporting plugins used by the project. |

`BeatMapManager` is the boundary between file storage and the editor. It reads `info.dat` into `BeatMapInfo`, loads the selected difficulty into `BeatMap`, and emits change signals consumed by the lane and editor. `BeatMap` keeps typed collections of notes, bombs, and walls alongside the original parsed JSON dictionary. Saving updates the relevant note and obstacle arrays in that original dictionary, preserving unrelated fields such as events and custom data, then serializes it to the difficulty file.

The lane listens for map changes and creates a Godot scene object for each data object. Those visual/runtime objects hold a reference to their `BeatMapObjectBase` data; edits update that data, while add/remove signals keep the scene and model in sync. The editor and playback services are autoloads, so UI and scene components communicate through shared services and signals rather than owning duplicate map state.

## Audio and visualization

`PlaybackManager` loads the map's Ogg Vorbis song, derives the playhead beat from BPM, and provides snapping and scrubbing. The spectrogram is generated asynchronously in `editor/Audio/SpectrogramAnalysis.cs`: audio windows are transformed with an FFT into frequency-band pixels, grouped into tiles, and requested/cached around the visible time range. `FloorSpectrogram` displays those tiles beneath the lane and updates a time marker. This lets the display request only nearby song data instead of analyzing and retaining the whole song at once.

## Data and current boundaries

- Maps are discovered and created in Beat Saber's `CustomWIPLevels` folder. `CustomLevelsFolder` is defined for the regular maps folder, but the primary editing list uses WIP maps.
- The map model accepts v2 files whose `_version` begins with `2`; unsupported versions fail to load.
- The map editor currently handles notes, bombs, and obstacles/walls in its data model. Other beatmap data such as events remains in the original JSON but is not exposed as an editable object collection.
- The map-creation/update UI currently configures Standard mode difficulties.
- `DebugWithoutVr` exists on the main scene script for non-headset development; normal launch requires initialized OpenXR before entering the editor.

## Where to look next

- `Main.cs` and `ui/MapList.cs`, `ui/MapDetails.cs` for startup, browsing, and map setup.
- `beat_saber/maps/BeatMapManager.cs` and `BeatMap.cs` for file loading, model changes, and saving.
- `editor/Editor.cs`, `InputManager.cs`, and `PlaybackManager.cs` for VR interaction and timing.
- `beat_saber/NoteBlockLane.cs` and `BeatmapObject.cs` for the relationship between beat data and 3D runtime objects.
- `editor/ObjectEditPlane/` for placement, selection, beat-grid, and spectrogram-related tools.
