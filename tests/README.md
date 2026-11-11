# Tests

All C# tests live in the separate `BSMapper.Tests` xUnit project, which references the application project. Run from the repository root:

```powershell
$env:GODOT_BIN = 'C:/Program Files/Godot/Godot_v4.7.2-stable_mono_win64_console.exe'
dotnet test BSMapper.slnx
```

Use the Godot **.NET** executable matching the application's SDK. `GODOT_BIN` is optional when `godot` is on `PATH`.

The xUnit cases cover selection ranges, additive selection, selector controls and native clicks, settings toggles, VR jump settings and save/reload, clipboard data, clipboard controls, editor integration, and autosaving. The autosave check waits for the real 30-second timer, exercises the VR save button, and verifies failure recovery and map-switch resets. Assertions use xUnit inside headless Godot because the application models, collections, and scenes require Godot's native runtime. Each case runs in a fresh process, with failures and engine output reported by xUnit. A missing engine or a timeout fails the test.

The fixture copies resources into an isolated directory beneath the test build output, imports them once, and removes the copy after the run. It does not modify the application's Godot configuration or assemblies. No headset is required; rendering and controller behavior still need manual checks.

To run a subset:

```powershell
dotnet test BSMapper.slnx --filter 'DisplayName~CopyPasteChecks'
dotnet test BSMapper.slnx --filter 'DisplayName~CheckSelectorClicks'
```

The GDScript map-details checks remain separate; see [map-details.md](map-details.md). Manual VR checklists are in [bulk-selection.md](bulk-selection.md), [copy-paste.md](copy-paste.md), and [headset-required.md](headset-required.md).
