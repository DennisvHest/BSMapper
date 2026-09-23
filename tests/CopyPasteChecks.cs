using System;
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
        var paste = panel.GetNode<Button>("%PasteButton");
        var copies = 0;
        var pastes = 0;
        panel.CopySelected += () => copies++;
        panel.PasteCopied += () => pastes++;
        Check(copy.Disabled && paste.Disabled, "Clipboard buttons initially disabled");
        panel.SetSelection(3, true);
        Check(!copy.Disabled && paste.Disabled, "Copy requires selection and Paste requires clipboard");
        copy.EmitSignal(Button.SignalName.Pressed);
        Check(copies == 1, "Copy button event");
        panel.SetSelection(0, false, 3);
        Check(copy.Disabled && !paste.Disabled && paste.Text == "Paste (3)", "Clipboard-only controls and count");
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
        plane.SetBulkSelectionModeEnabled(true);
        editor.UpdateBulkSelection(sources);
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

        playback.SetPlaybackPosition(10.0);
        panelUi.GetNode<Button>("%PasteButton").EmitSignal(Button.SignalName.Pressed);
        Check(map.Notes.Count == 2 && map.Bombs.Count == 2 && map.Walls.Count == 2, "Real panel Paste adds all types");
        Check(map.Notes[1].Beat == 20.0 && map.Bombs[1].Beat == 21.5 && map.Walls[1].Beat == 22.0,
            "Paste uses current playback beat with preserved spacing");
        Check(map.Notes[1].Type == BeatMapNote.NoteBlockType.Left, "Editor paste uses snapshot rather than edited source");
        var pastedObjects = lane.GetChildren().OfType<BeatmapObject>().Except(sources).ToArray();
        Check(pastedObjects.Length == 3 && pastedObjects.All(obj => !obj.IsCopied && !obj.IsSelected),
            "Pasted nodes render as independent unselected objects");
        Check(pastedObjects.All(obj => GetOutlineMaterial(obj).AlbedoColor.G < 0.9f), "Copied outlines do not leak to new instances");
        playback.SetPlaybackPosition(5.0);
        editor.PasteCopiedObjects();
        Check(map.Notes.Count == 3 && map.Notes[2].Beat == 10.0 && map.Bombs[2].Beat == 11.5,
            "Editor supports repeated paste at an earlier beat");

        var nextSource = pastedObjects.First(obj => obj is NoteBlock);
        editor.UpdateBulkSelection(new[] { nextSource });
        editor.CopySelectedObjects();
        Check(editor.ClipboardCount == 1 && nextSource.IsCopied && sources.All(source => !source.IsCopied),
            "Replacing clipboard updates source markers");
        Check(sources.Select((source, index) => GetOutlineMaterial(source).AlbedoColor == originalColors[index]).All(value => value),
            "Replacing clipboard restores original outline colors");
        editor.DeleteSelectedObjects();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(editor.ClipboardCount == 1 && panel.Visible, "Deleting source retains clipboard and panel");
        playback.SetPlaybackPosition(1.0);
        editor.PasteCopiedObjects();
        Check(map.Notes.Last().Beat == 2.0, "Deleted source can still be pasted from snapshot");

        var replacement = new BeatMap();
        replacement.InitializeEmpty();
        manager.CurrentBeatmap = replacement;
        manager.EmitSignal(BeatMapManager.SignalName.CurrentBeatmapChanged, replacement);
        Check(editor.ClipboardCount == 0 && !panel.Visible, "Changing maps clears clipboard and panel");
        editor.PasteCopiedObjects();
        Check(replacement.Notes.Count == 0, "Empty clipboard paste is a no-op");
        editor.Free();
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
