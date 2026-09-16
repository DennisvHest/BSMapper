# Headset-required editor entry

## Runtime diagnosis

With no headset connected, Godot 4.7 / VirtualDesktopXR 1.0.10 reported
`XR_ERROR_FORM_FACTOR_UNAVAILABLE` during engine startup and continued in normal
mode. At `Main.Start`, the OpenXR interface existed but `IsInitialized()` and the
viewport's `UseXR` were both false. A debugger-invoked `Initialize()` returned
false as well. Previously, `Start` changed to the editor scene regardless, leaving
XR disabled with no subsequent initialization or viewport-enable path.

Subsequent debugging confirmed that connecting through Virtual Desktop and then
calling `Initialize()` still returned false. Native instance, system, and session
handles remained zero. A fresh launch with the headset connected initialized
OpenXR and rendered the editor in VR successfully.

The headset message now offers **Restart and open editor** and **Cancel**.
Confirmation requests a Godot process restart with the selected map info path and
difficulty filename in application-specific user arguments. The new process
reloads that difficulty and enters the editor without another selection step.
No resume state is written to settings: unrelated normal launches still open the
map screen. Existing engine/user arguments are preserved; previous resume
arguments are replaced on subsequent restarts.

If XR is still unavailable after restart, the confirmation appears again and
waits for user input, never automatically restarting. Missing or invalid resume
targets display an error and leave the map screen accessible. This does not add
in-process XR recovery or in-editor reconnect handling. The initialization check
is not a headset-wear sensor: an initialized but unfocused session retains the
existing behavior.

## Manual regression checks

1. Start without a headset, dismiss Godot's startup alert if present, create a
   map, and open its editor. Expect a **Please put on your headset** dialog with
   Virtual Desktop connection and restart guidance. The editor must not open.
2. Dismiss with Cancel, Escape, or the window close button. The process must not
   restart and the map screen must remain usable. Open the map again: the same
   dialog should appear without duplicate windows.
3. Repeat with an existing map that has several difficulties. Select a difficulty
   and verify the same headset dialog appears without entering the editor.
4. While the confirmation is open, connect using Virtual Desktop and wait until
   the PC connection is ready. Click **Restart and open editor**. Expect a new
   process to load the same map and selected difficulty directly into VR, with no
   map or difficulty picker. Verify headset rendering and controller input.
   Repeat with paths containing spaces and non-ASCII characters, and a map with
   several difficulties (select one other than the first).
5. Start with a connected headset and initialized OpenXR. Opening either a
   single- or multiple-difficulty map should enter VR without the dialog.
6. With `Main.DebugWithoutVr` enabled in a local debug scene, start without a
   headset and open a map. Expect desktop editor entry without the headset dialog
   or the misleading `OpenXR not initialized` message from `Main.Start`. Restore
   the debug flag afterward.

7. Confirm while still disconnected. After the restart, expect the confirmation
   again, not a restart loop or a non-VR editor. Connect and confirm a second time;
   the correct map/difficulty should still be restored.
8. Exit the application normally after a successful restart. Launch it normally
   again: expect the map screen, not an automatic resume or another restart.
9. Test a resume request whose map/difficulty file is missing or invalid. Expect
   **Unable to reopen map**, a log explaining the failure, and a usable map screen.
10. Verify the restart both when running the unexported project and in an exported
    desktop build. Confirm unrelated user arguments survive repeated restarts.

Hardware-dependent checks require a real headset. The restarted process may need
to be attached separately in Visual Studio to inspect its loaded map/difficulty.
Godot documents `OS.SetRestartOnExit` as supported on desktop when the project is
not launched from the Godot editor. Use a direct project launch (as in the Visual
Studio verification) or an exported desktop application for this flow.

## Validation performed

- Launched the rebuilt application under the Visual Studio debugger and reached
  the new early return after `PopupCentered()` on the no-headset path.
- Confirmed the dialog's `Visible` property was true, its text contained the
  headset/connection guidance, the current scene remained `res://main.tscn`, and
  viewport XR remained disabled.
- The original message-only fix was verified; it did not implement recovery.
- Connected-at-startup VR was confirmed by the user in the subsequent diagnosis.
- Built/launched the changed application through Visual Studio. Confirming the
  dialog exited the original process with code 0 and created a new process.
- Attached to the restarted process and verified `res://editor/editor.tscn`, the
  same map info path (containing spaces), and `ExpertStandard.dat`. OpenXR was
  initialized and viewport XR was enabled. `OS.IsRestartOnExitSet()` was false,
  so the new process would not restart again on a normal exit.
- The user confirmed the fix through the verification dialog after resuming.
- Exported builds, cancellation, still-disconnected repeated restarts, malformed
  resume requests, non-ASCII paths, and unrelated argument preservation remain
  manual regression checks. No automated headset tests were run.
