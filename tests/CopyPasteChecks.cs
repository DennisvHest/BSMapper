using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Dictionary = Godot.Collections.Dictionary;

public partial class CopyPasteChecks : Node
{
    private int _checks;

    public override void _Ready()
    {
        Callable.From(Run).CallDeferred();
    }

    private async void Run()
    {
        try
        {
            CheckClipboardData();
            CheckPanelControls();
            await CheckEditorIntegration();
            GD.Print($"Copy/paste checks passed: {_checks}");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private void CheckClipboardData()
    {
        var clipboard = new BeatmapClipboard();
        Check(clipboard.Count == 0 && clipboard.CreatePaste(4.0).Count == 0, "Empty clipboard");
        var custom = new Dictionary { ["label"] = "original", ["values"] = new Godot.Collections.Array { 1, 2 } };
        using var note = new BeatMapNote
        {
            Beat = 8.5, LineIndex = 3, LineLayer = 2,
            Type = BeatMapNote.NoteBlockType.Right, Cut = BeatMapNote.CutDirection.UpLeft,
            OriginalObject = new Dictionary { ["_customData"] = custom, ["_time"] = 8.5 },
        };
        using var bomb = new BeatMapBomb { Beat = 10.0, LineIndex = 1, LineLayer = 0 };
        using var wall = new BeatMapWall
        {
            Beat = 7.25, LineIndex = 1, LineLayer = 1, Width = 2, Height = 4,
            Type = BeatMapWall.WallType.Free, Duration = 3.75,
        };
        clipboard.Copy(new BeatMapObjectBase[] { note, bomb, wall });
        Check(clipboard.Count == 3, "Copy includes all object types");
        note.LineIndex = 0;
        note.Type = BeatMapNote.NoteBlockType.Left;
        note.Cut = BeatMapNote.CutDirection.Any;
        note.Beat = 99.0;
        wall.Duration = 1.0;
        custom["label"] = "changed";
        custom["values"].AsGodotArray()[0] = 99;

        var paste = clipboard.CreatePaste(20.0);
        var pastedNote = (BeatMapNote)paste[0];
        var pastedBomb = (BeatMapBomb)paste[1];
        var pastedWall = (BeatMapWall)paste[2];
        Check(pastedWall.Beat == 20.0 && pastedNote.Beat == 21.25 && pastedBomb.Beat == 22.75,
            "Earliest selected beat anchors paste regardless of enumeration order");
        Check(pastedNote.LineIndex == 3 && pastedNote.LineLayer == 2
            && pastedNote.Type == BeatMapNote.NoteBlockType.Right && pastedNote.Cut == BeatMapNote.CutDirection.UpLeft,
            "Note snapshot preserves position, color and direction despite later source edits");
        Check(pastedBomb.LineIndex == 1 && pastedBomb.LineLayer == 0, "Bomb grid position");
        Check(pastedWall.Width == 2 && pastedWall.Height == 4 && pastedWall.LineLayer == 1
            && pastedWall.Type == BeatMapWall.WallType.Free && pastedWall.Duration == 3.75, "Wall snapshot fields");
        var copiedCustom = pastedNote.OriginalObject.AsGodotDictionary()["_customData"].AsGodotDictionary();
        Check(copiedCustom["label"].AsString() == "original"
            && copiedCustom["values"].AsGodotArray()[0].AsInt32() == 1, "Deep snapshot metadata");
        copiedCustom["label"] = "paste edited";
        copiedCustom["values"].AsGodotArray()[0] = 55;
        pastedNote.LineIndex = 2;
        var again = clipboard.CreatePaste(0.0);
        var repeatedNote = (BeatMapNote)again[0];
        var repeatedCustom = repeatedNote.OriginalObject.AsGodotDictionary()["_customData"].AsGodotDictionary();
        Check(again[2].Beat == 0.0 && repeatedNote.Beat == 1.25, "Paste earlier at song start");
        Check(!ReferenceEquals(pastedNote, repeatedNote) && repeatedNote.LineIndex == 3
            && repeatedCustom["label"].AsString() == "original"
            && repeatedCustom["values"].AsGodotArray()[0].AsInt32() == 1, "Repeated paste has independent data and metadata");

        using var map = new BeatMap();
        map.InitializeEmpty();
        var additions = 0;
        map.ObjectAdded += _ => additions++;
        foreach (var data in again)
        {
            map.AddObject(data);
        }
        map.SaveChanges();
        Check(additions == 3 && map.Notes.Count == 1 && map.Bombs.Count == 1 && map.Walls.Count == 1,
            "Normal map addition path receives every clone");
        Check(note.OriginalObject.AsGodotDictionary()["_time"].AsDouble() == 8.5, "Saving copies does not mutate original serialized data");
        using var reloaded = new BeatMap();
        reloaded.LoadFromFile(map.OriginalMap);
        Check(reloaded.Notes[0].Beat == 1.25 && reloaded.Bombs[0].Beat == 2.75, "Note and bomb save/reload offsets");
        Check(reloaded.Walls[0].Beat == 0.0 && reloaded.Walls[0].LineLayer == 1
            && reloaded.Walls[0].Height == 4 && reloaded.Walls[0].Width == 2
            && reloaded.Walls[0].Duration == 3.75, "Wall save/reload dimensions and duration");
        clipboard.Copy(new[] { bomb });
        Check(clipboard.Count == 1 && clipboard.CreatePaste(12.0)[0].Beat == 12.0, "New copy replaces clipboard");
        clipboard.Clear();
        Check(clipboard.Count == 0 && clipboard.CreatePaste(0.0).Count == 0, "Clipboard clear");
    }

    private void CheckPanelControls()
    {
        var panel = GD.Load<PackedScene>("res://editor/ObjectEditPlane/selection_panel_ui.tscn")
            .Instantiate<SelectionPanelUI>();
        AddChild(panel);
        var copy = panel.GetNode<Button>("%CopyButton");
        var cut = panel.GetNode<Button>("%CutButton");
        var paste = panel.GetNode<Button>("%PasteButton");
        var moveBack = panel.GetNode<Button>("%MoveBackButton");
        var moveForward = panel.GetNode<Button>("%MoveForwardButton");
        var copies = 0;
        var cuts = 0;
        var pastes = 0;
        var moves = new List<int>();
        panel.CopySelected += () => copies++;
        panel.CutSelected += () => cuts++;
        panel.PasteCopied += () => pastes++;
        panel.MoveSelectedBySubdivision += direction => moves.Add(direction);
        Check(copy.Disabled && cut.Disabled && paste.Disabled && moveBack.Disabled && moveForward.Disabled,
            "Clipboard and movement buttons initially disabled");
        panel.SetSelection(3, true);
        Check(!copy.Disabled && !cut.Disabled && paste.Disabled && !moveBack.Disabled && !moveForward.Disabled,
            "Copy/Cut and movement require selection; Paste requires clipboard");
        copy.EmitSignal(Button.SignalName.Pressed);
        Check(copies == 1, "Copy button event");
        cut.EmitSignal(Button.SignalName.Pressed);
        Check(cuts == 1, "Cut button event");
        moveBack.EmitSignal(Button.SignalName.Pressed);
        moveForward.EmitSignal(Button.SignalName.Pressed);
        Check(moves.SequenceEqual(new[] { -1, 1 }), "Back/Forth button directions");
        panel.SetSelection(0, false, 3);
        Check(copy.Disabled && cut.Disabled && !paste.Disabled && moveBack.Disabled && moveForward.Disabled
            && paste.Text == "Paste (3)",
            "Clipboard-only controls and count");
        Check(panel.GetNode<Button>("%DeleteButton").Disabled && panel.GetNode<Button>("%DeselectButton").Disabled,
            "Selection actions disabled without selection");
        paste.EmitSignal(Button.SignalName.Pressed);
        Check(pastes == 1, "Paste button event");
        panel.Free();
    }

    private async Task CheckEditorIntegration()
    {
        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        var playback = GetNode<PlaybackManager>("/root/PlaybackManager");
        var map = new BeatMap();
        map.InitializeEmpty();
        map.AddObject(new BeatMapNote
        {
            Beat = 4.0, LineIndex = 0, LineLayer = 0,
            Type = BeatMapNote.NoteBlockType.Left, Cut = BeatMapNote.CutDirection.Down,
        });
        map.AddObject(new BeatMapBomb { Beat = 5.5, LineIndex = 1, LineLayer = 1 });
        map.AddObject(new BeatMapWall
        {
            Beat = 6.0, LineIndex = 2, LineLayer = 0, Width = 1, Height = 5, Duration = 2.0,
        });
        manager.CurrentBeatmap = map;
        manager.CurrentBeatmapDifficultyInfo = new BeatMapDifficultyInfo { Bpm = 120, Njs = 10 };
        manager.CurrentBeatmapDifficultyInfo.Initialize();
        manager.EmitSignal(BeatMapManager.SignalName.CurrentBeatmapDifficultyInfoChanged, manager.CurrentBeatmapDifficultyInfo);
        playback.Music.Stream = new AudioStreamWav
        {
            MixRate = 8000, Format = AudioStreamWav.FormatEnum.Format8Bits, Data = new byte[480000],
        };
        var editor = GD.Load<PackedScene>("res://editor/editor.tscn").Instantiate<Editor>();
        GetTree().Root.AddChild(editor);
        playback.ChangeMode(PlaybackManager.EditMode.Editing);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var lane = editor.GetNode<NoteBlockLane>("NoteBlockLane");
        var plane = lane.GetNode<ObjectEditPlane>("ObjectEditPlane");
        var sources = lane.GetChildren().OfType<BeatmapObject>().ToArray();
        var panel = lane.GetNode<SelectionPanel>("SelectionPanel");
        var panelUi = (SelectionPanelUI)panel.GetNode<Node>("ViewportPanel").Call("get_scene_instance").AsGodotObject();
        var originalColors = sources.Select(GetOutlineMaterial).Select(material => material.AlbedoColor).ToArray();
        var sourceBeats = sources.Select(source => source.BeatmapData.Beat).ToArray();
        plane.SetBulkSelectionModeEnabled(true);
        editor.UpdateBulkSelection(sources);
        playback.SetBeatSubdivision(4);
        panelUi.GetNode<Button>("%MoveForwardButton").EmitSignal(Button.SignalName.Pressed);
        Check(sources.Select((source, index) => source.BeatmapData.Beat == sourceBeats[index] + 0.25).All(value => value)
            && sources.All(source => Mathf.IsEqualApprox(source.ObjectTime,
                (float)(source.BeatmapData.Beat / manager.CurrentBeatmapDifficultyInfo.Bpm * 60.0))),
            "Forward moves selected note, bomb and wall by the current quarter subdivision");
        Check(sources.All(source => source.IsSelected), "Subdivision move retains selection");
        playback.SetBeatSubdivision(1);
        panelUi.GetNode<Button>("%MoveBackButton").EmitSignal(Button.SignalName.Pressed);
        Check(sources.Select((source, index) => source.BeatmapData.Beat == sourceBeats[index] - 0.75).All(value => value)
            && sources.All(source => Mathf.IsEqualApprox(source.ObjectTime,
                (float)(source.BeatmapData.Beat / manager.CurrentBeatmapDifficultyInfo.Bpm * 60.0))),
            "Back moves selected objects by one whole beat and refreshes visual timing");
        panelUi.GetNode<Button>("%CopyButton").EmitSignal(Button.SignalName.Pressed);
        Check(editor.ClipboardCount == 3 && sources.All(source => source.IsCopied), "Real panel Copy captures and marks selection");
        Check(sources.All(source => GetOutlineMaterial(source).AlbedoColor.G > 0.9f
            && GetOutlineMaterial(source).Emission.G > 0.9f), "All object types get green albedo and emission");
        editor.UpdateBulkSelection(Array.Empty<BeatmapObject>());
        Check(sources.All(source => source.IsSelected), "Copy commits cube-owned selection before scrubbing");
        ((NoteBlock)sources.First(source => source is NoteBlock)).SetNoteBlockType(BeatMapNote.NoteBlockType.Right);
        editor.DeselectAllObjects();
        Check(sources.All(source => source.IsCopied && !source.IsSelected)
            && sources.All(source => source.GetNode<MeshInstance3D>("Visual/HighlightOutline").Visible),
            "Copied source outlines survive deselection");
        Check(panel.Visible && !panelUi.GetNode<Button>("%PasteButton").Disabled, "Clipboard keeps the real panel available");

        editor.UpdateBulkSelection(sources.Take(1));
        playback.SetPlaybackPosition(10.0);
        panelUi.GetNode<Button>("%PasteButton").EmitSignal(Button.SignalName.Pressed);
        Check(map.Notes.Count == 2 && map.Bombs.Count == 2 && map.Walls.Count == 2, "Real panel Paste adds all types");
        Check(map.Notes[1].Beat == 20.0 && map.Bombs[1].Beat == 21.5 && map.Walls[1].Beat == 22.0,
            "Paste uses current playback beat with preserved spacing");
        Check(map.Notes[1].Type == BeatMapNote.NoteBlockType.Left, "Editor paste uses snapshot rather than edited source");
        var pastedObjects = lane.GetChildren().OfType<BeatmapObject>().Except(sources).ToArray();
        Check(pastedObjects.Length == 3 && pastedObjects.All(obj => obj.IsCopied && obj.IsSelected),
            "Pasted notes, bombs and walls become selected and copied");
        Check(pastedObjects.All(obj => GetOutlineMaterial(obj).AlbedoColor.G > 0.9f
            && GetOutlineMaterial(obj).Emission.G > 0.9f), "Pasted objects have green outlines");
        Check(sources.All(obj => !obj.IsSelected && !obj.IsCopied), "Paste clears previous selection and copied markers");
        Check(panelUi.GetNode<Label>("%Title").Text == "Selected: 3"
            && !panelUi.GetNode<Button>("%CopyButton").Disabled
            && !panelUi.GetNode<Button>("%DeleteButton").Disabled, "Panel actions target the newly pasted selection");
        Check(editor.ClipboardCount == 3, "Pasted group remains available in the clipboard");
        playback.SetPlaybackPosition(5.0);
        editor.PasteCopiedObjects();
        Check(map.Notes.Count == 3 && map.Notes[2].Beat == 10.0 && map.Bombs[2].Beat == 11.5,
            "Editor supports repeated paste at an earlier beat");
        var repeatedObjects = lane.GetChildren().OfType<BeatmapObject>().Except(sources).Except(pastedObjects).ToArray();
        Check(repeatedObjects.Length == 3 && repeatedObjects.All(obj => obj.IsSelected && obj.IsCopied)
            && pastedObjects.All(obj => !obj.IsSelected && !obj.IsCopied), "Repeated paste transfers both states to the newest group");
        Check(pastedObjects.All(obj => GetOutlineMaterial(obj).AlbedoColor.G < 0.9f), "Previous pasted outlines restore their normal color");

        editor.PasteCopiedObjects();
        var sameBeatObjects = lane.GetChildren().OfType<BeatmapObject>()
            .Except(sources).Except(pastedObjects).Except(repeatedObjects).ToArray();
        Check(sameBeatObjects.Length == 3 && sameBeatObjects.All(obj => obj.IsSelected && obj.IsCopied)
            && repeatedObjects.All(obj => !obj.IsSelected && !obj.IsCopied), "Same-beat paste selects only the new object identities");
        editor.UpdateBulkSelection(Array.Empty<BeatmapObject>());
        Check(sameBeatObjects.All(obj => obj.IsSelected && obj.IsCopied), "Pasted selection is not owned by an old live cube");

        editor.DeselectAllObjects();
        var nextSource = pastedObjects.First(obj => obj is NoteBlock);
        editor.UpdateBulkSelection(new[] { nextSource });
        editor.CopySelectedObjects();
        Check(editor.ClipboardCount == 1 && nextSource.IsCopied && sources.All(source => !source.IsCopied)
            && sameBeatObjects.All(obj => !obj.IsCopied),
            "Replacing clipboard updates source markers");
        Check(sources.Select((source, index) => GetOutlineMaterial(source).AlbedoColor == originalColors[index]).All(value => value),
            "Replacing clipboard restores original outline colors");
        editor.DeleteSelectedObjects();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(editor.ClipboardCount == 1 && panel.Visible, "Deleting source retains clipboard and panel");
        playback.SetPlaybackPosition(1.0);
        editor.PasteCopiedObjects();
        Check(map.Notes.Last().Beat == 2.0, "Deleted source can still be pasted from snapshot");
        Check(lane.GetChildren().OfType<BeatmapObject>()
            .Single(obj => obj.BeatmapData == map.Notes.Last()) is { IsSelected: true, IsCopied: true },
            "Paste from a deleted source selects and marks the replacement");

        editor.DeselectAllObjects();
        editor.UpdateBulkSelection(sameBeatObjects);
        var cutData = sameBeatObjects.Select(obj => obj.BeatmapData).ToHashSet();
        var cutVisualAlphas = sameBeatObjects.Select(GetVisualMaterial).Select(material => material.AlbedoColor.A).ToArray();
        var noteCountBeforeCut = map.Notes.Count;
        var bombCountBeforeCut = map.Bombs.Count;
        var wallCountBeforeCut = map.Walls.Count;
        panelUi.GetNode<Button>("%CutButton").EmitSignal(Button.SignalName.Pressed);
        Check(editor.ClipboardCount == 3 && sameBeatObjects.All(obj => obj.IsCut && obj.IsSelected && !obj.IsCopied),
            "Real panel Cut captures selected sources without deleting them yet");
        Check(sameBeatObjects.Select((obj, index) => Mathf.IsEqualApprox(
            GetVisualMaterial(obj).AlbedoColor.A, cutVisualAlphas[index] * 0.25f)).All(value => value),
            "Cut note, bomb and wall visuals are semitransparent");
        Check(map.Notes.Count == noteCountBeforeCut && map.Bombs.Count == bombCountBeforeCut
            && map.Walls.Count == wallCountBeforeCut, "Cut leaves source map data intact before Paste");
        editor.DeselectAllObjects();
        playback.SetPlaybackPosition(15.0);
        panelUi.GetNode<Button>("%PasteButton").EmitSignal(Button.SignalName.Pressed);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var cutReplacements = lane.GetChildren().OfType<BeatmapObject>()
            .Where(obj => !cutData.Contains(obj.BeatmapData) && obj.IsSelected && obj.IsCopied).ToArray();
        Check(map.Notes.Count == noteCountBeforeCut && map.Bombs.Count == bombCountBeforeCut
            && map.Walls.Count == wallCountBeforeCut && cutData.All(data => !Contains(map, data)),
            "Paste deletes cut sources after adding replacements");
        Check(cutReplacements.Length == 3 && cutReplacements.All(obj => !obj.IsCut),
            "Cut Paste selects and marks the replacement group without cut state");

        var replacement = new BeatMap();
        replacement.InitializeEmpty();
        manager.CurrentBeatmap = replacement;
        manager.EmitSignal(BeatMapManager.SignalName.CurrentBeatmapChanged, replacement);
        Check(editor.ClipboardCount == 0 && !panel.Visible, "Changing maps clears clipboard and panel");
        editor.PasteCopiedObjects();
        Check(replacement.Notes.Count == 0, "Empty clipboard paste is a no-op");
        editor.Free();
    }

    private static StandardMaterial3D GetVisualMaterial(BeatmapObject obj)
    {
        return (StandardMaterial3D)obj.GetNode<MeshInstance3D>("Visual/MeshInstance3D").GetActiveMaterial(0);
    }

    private static bool Contains(BeatMap map, BeatMapObjectBase data)
    {
        return data switch
        {
            BeatMapNote note => map.Notes.Contains(note),
            BeatMapBomb bomb => map.Bombs.Contains(bomb),
            BeatMapWall wall => map.Walls.Contains(wall),
            _ => false,
        };
    }

    private static StandardMaterial3D GetOutlineMaterial(BeatmapObject obj)
    {
        return (StandardMaterial3D)obj.GetNode<MeshInstance3D>("Visual/HighlightOutline").GetActiveMaterial(0);
    }

    private void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name);
        }
        _checks++;
    }
}
