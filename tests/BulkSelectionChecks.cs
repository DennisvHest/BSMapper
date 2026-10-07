using System;
using System.Threading.Tasks;
using Godot;

public partial class BulkSelectionChecks : Node
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
            CheckRanges();
            CheckAdditiveSelection();
            CheckSelector();
            await CheckSelectorClicks();
            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--object-selector-only") < 0)
            {
                CheckSettingsToggles();
            }
            GD.Print($"Bulk selection checks passed: {_checks}");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private void CheckRanges()
    {
        var forward = new BulkSelectionRange(1, 0, 2, 1, 4.0, 8.0);
        var reverse = new BulkSelectionRange(2, 1, 1, 0, 8.0, 4.0);
        using var note = new BeatMapNote { LineIndex = 1, LineLayer = 0, Beat = 4.0 };
        Check(forward.Contains(note) && reverse.Contains(note), "Inclusive starting beat and reversed corners");
        note.Beat = 8.0;
        Check(forward.Contains(note), "Inclusive ending beat");
        note.Beat = 8.01;
        Check(!forward.Contains(note), "Outside ending beat");
        note.Beat = 3.99;
        Check(!forward.Contains(note), "Outside starting beat");
        note.Beat = 6.0;
        note.LineIndex = 0;
        Check(!forward.Contains(note), "Outside columns");
        note.LineIndex = 2;
        note.LineLayer = 2;
        Check(!forward.Contains(note), "Outside rows");
        using var bomb = new BeatMapBomb { LineIndex = 2, LineLayer = 1, Beat = 6.0 };
        Check(forward.Contains(bomb) && reverse.Contains(bomb), "Bomb inside cube");
        var zeroDepth = new BulkSelectionRange(2, 1, 2, 1, 6.0, 6.0);
        Check(zeroDepth.Contains(bomb), "Single cell at zero depth");
        var shrink = new BulkSelectionRange(1, 0, 2, 1, 4.0, 5.0);
        Check(!shrink.Contains(bomb), "Shrinking removes previously crossed objects");
        var pastAnchor = new BulkSelectionRange(1, 0, 2, 1, 4.0, 2.0);
        note.LineIndex = 1;
        note.LineLayer = 0;
        note.Beat = 3.0;
        Check(pastAnchor.Contains(note) && !pastAnchor.Contains(bomb), "Scrubbing through anchor reverses interval");
        using var wall = new BeatMapWall
        {
            LineIndex = 0, LineLayer = 0, Width = 2, Height = 5, Beat = 2.0, Duration = 3.0,
        };
        Check(forward.Contains(wall), "Wall cells and duration overlap despite origin outside cube");
        wall.Duration = 1.0;
        Check(!forward.Contains(wall), "Wall ends before cube");
        wall.Duration = 3.0;
        wall.Type = BeatMapWall.WallType.Crouch;
        wall.Height = 3;
        Check(!forward.Contains(wall), "Crouch wall excludes lower rows");
        Check(new BulkSelectionRange(1, 2, 1, 2, 4.0, 8.0).Contains(wall), "Crouch wall occupies upper row");
        wall.Width = 0;
        Check(!forward.Contains(wall), "Empty wall is excluded");
    }

    private void CheckAdditiveSelection()
    {
        var editor = new Editor();
        var prior = CreateObject();
        var first = CreateObject();
        var second = CreateObject();
        var count = -1;
        var notifications = 0;
        editor.SelectionChanged += (selectedCount, _) => { count = selectedCount; notifications++; };
        editor.UpdateBulkSelection(new[] { prior });
        editor.CommitBulkSelection();
        editor.UpdateBulkSelection(new[] { prior, first, second });
        Check(count == 3 && prior.IsSelected && first.IsSelected && second.IsSelected, "Additive expansion");
        var previousNotifications = notifications;
        editor.UpdateBulkSelection(new[] { prior, first, second });
        Check(notifications == previousNotifications, "Unchanged selection does not emit redundant notifications");
        editor.UpdateBulkSelection(new[] { first });
        Check(count == 2 && prior.IsSelected && first.IsSelected && !second.IsSelected, "Shrink preserves prior selection");
        editor.UpdateBulkSelection(Array.Empty<BeatmapObject>());
        Check(count == 1 && prior.IsSelected && !first.IsSelected, "Empty cube preserves baseline");
        editor.UpdateBulkSelection(new[] { first });
        editor.CommitBulkSelection();
        editor.UpdateBulkSelection(new[] { second });
        editor.UpdateBulkSelection(Array.Empty<BeatmapObject>());
        Check(count == 2 && prior.IsSelected && first.IsSelected, "New cube preserves committed previous cube");
        first.Free();
        Check(count == 1, "Tree exit removes selected object");
        editor.DeselectAllObjects();
        Check(count == 0 && !prior.IsSelected, "Explicit deselection clears all ownership");
        editor.UpdateBulkSelection(new[] { prior, second });
        editor.DeleteSelectedObjects();
        Check(count == 0 && prior.IsQueuedForDeletion() && second.IsQueuedForDeletion(), "Delete clears selection and queues objects");
        prior.Free();
        second.Free();
        editor.Free();
    }

    private BeatmapObject CreateObject()
    {
        var result = new BeatmapObject { ProcessMode = ProcessModeEnum.Disabled };
        AddChild(result);
        return result;
    }

    private void CheckSelector()
    {
        var scene = GD.Load<PackedScene>("res://editor/ObjectEditPlane/object_type_selector_ui.tscn");
        var selector = scene.Instantiate<ObjectTypeSelectorUI>();
        AddChild(selector);
        var button = selector.GetNode<Button>("%BulkSelectionButton");
        var noteButton = selector.GetNode<Button>("%NoteButton");
        var anyDirectionButton = selector.GetNode<Button>("%AnyDirectionNoteButton");
        var bombButton = selector.GetNode<Button>("%BombButton");
        foreach (var toggle in new[] { button, noteButton, anyDirectionButton, bombButton })
        {
            CheckToggleTheme(toggle);
        }
        Check(noteButton.ButtonGroup is not null && noteButton.ButtonGroup == anyDirectionButton.ButtonGroup
            && noteButton.ButtonGroup == bombButton.ButtonGroup, "Placement tools share an exclusive group");
        Check(!noteButton.ButtonGroup.AllowUnpress && button.ButtonGroup is null,
            "Placement choice stays selected and bulk toggle is independent");
        var requests = 0;
        var toggles = 0;
        selector.BulkSelectionToggled += () => requests++;
        button.Toggled += _ => toggles++;
        Check(!button.ButtonPressed && noteButton.ButtonPressed, "Bulk mode initially off with Note selected");
        button.EmitSignal(Button.SignalName.Pressed);
        Check(requests == 1, "VR button emits toggle request");
        selector.SetBulkSelectionEnabled(true);
        Check(((IconButton)button).IconName == "table-cells-large", "Bulk selection retains its scene icon");
        Check(button.ButtonPressed, "Enabled highlight uses pressed state");
        Check(!noteButton.ButtonPressed && !anyDirectionButton.ButtonPressed && !bombButton.ButtonPressed,
            "Placement highlight suppressed in bulk mode");
        selector.SetBulkSelectionEnabled(false);
        Check(!button.ButtonPressed && noteButton.ButtonPressed, "Placement highlight restored");
        selector.SetSelectedObjectType(ObjectEditPlane.PlaceableObjectType.Bomb);
        Check(bombButton.ButtonPressed && !noteButton.ButtonPressed && !anyDirectionButton.ButtonPressed,
            "Bomb selection uses exclusive pressed state");
        selector.SetSelectedObjectType(ObjectEditPlane.PlaceableObjectType.AnyDirectionNoteBlock);
        Check(anyDirectionButton.ButtonPressed && !noteButton.ButtonPressed && !bombButton.ButtonPressed,
            "Any-direction selection uses exclusive pressed state");
        Check(requests == 1 && toggles == 0, "State synchronization emits no toggle requests or signals");
        selector.Free();
    }

    private async Task CheckSelectorClicks()
    {
        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        var playback = GetNode<PlaybackManager>("/root/PlaybackManager");
        var map = new BeatMap();
        map.InitializeEmpty();
        manager.CurrentBeatmap = map;
        manager.CurrentBeatmapDifficultyInfo = new BeatMapDifficultyInfo { Bpm = 120, Njs = 10 };
        manager.CurrentBeatmapDifficultyInfo.Initialize();
        manager.EmitSignal(BeatMapManager.SignalName.CurrentBeatmapDifficultyInfoChanged,
            manager.CurrentBeatmapDifficultyInfo);
        var editor = GD.Load<PackedScene>("res://editor/editor.tscn").Instantiate<Editor>();
        GetTree().Root.AddChild(editor);
        playback.ChangeMode(PlaybackManager.EditMode.Editing);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var plane = editor.GetNode<ObjectEditPlane>("NoteBlockLane/ObjectEditPlane");
        var panel = plane.GetNode<Node>("ObjectTypeSelector/ViewportPanel");
        var selector = (ObjectTypeSelectorUI)panel.Call("get_scene_instance").AsGodotObject();
        var viewport = panel.GetNode<SubViewport>("Viewport");
        var note = selector.GetNode<Button>("%NoteButton");
        var anyDirection = selector.GetNode<Button>("%AnyDirectionNoteButton");
        var bomb = selector.GetNode<Button>("%BombButton");
        var bulk = selector.GetNode<Button>("%BulkSelectionButton");
        var selectionChanges = 0;
        plane.SelectedObjectTypeChanged += _ => selectionChanges++;

        void CheckState(ObjectEditPlane.PlaceableObjectType type, bool bulkEnabled)
        {
            Check(plane.SelectedObjectType == type && plane.BulkSelectionModeEnabled == bulkEnabled,
                "Native click synchronizes application selection and bulk mode");
            Check(note.ButtonPressed == (!bulkEnabled && type == ObjectEditPlane.PlaceableObjectType.NoteBlock)
                && anyDirection.ButtonPressed == (!bulkEnabled && type == ObjectEditPlane.PlaceableObjectType.AnyDirectionNoteBlock)
                && bomb.ButtonPressed == (!bulkEnabled && type == ObjectEditPlane.PlaceableObjectType.Bomb)
                && bulk.ButtonPressed == bulkEnabled, "Native press/release retains exactly the selected highlight");
        }

        CheckState(ObjectEditPlane.PlaceableObjectType.NoteBlock, false);
        await Click(bomb);
        CheckState(ObjectEditPlane.PlaceableObjectType.Bomb, false);
        var changesBeforeRepeat = selectionChanges;
        await Click(bomb);
        CheckState(ObjectEditPlane.PlaceableObjectType.Bomb, false);
        Check(selectionChanges == changesBeforeRepeat, "Repeated tool click retains highlight without selection notification");
        await Click(bulk);
        CheckState(ObjectEditPlane.PlaceableObjectType.Bomb, true);
        await Click(bomb);
        CheckState(ObjectEditPlane.PlaceableObjectType.Bomb, false);
        Check(selectionChanges == changesBeforeRepeat, "Choosing the same tool exits bulk without selection notification");
        await Click(anyDirection);
        CheckState(ObjectEditPlane.PlaceableObjectType.AnyDirectionNoteBlock, false);
        await Click(note);
        CheckState(ObjectEditPlane.PlaceableObjectType.NoteBlock, false);
        await Click(bulk);
        CheckState(ObjectEditPlane.PlaceableObjectType.NoteBlock, true);
        await Click(bulk);
        CheckState(ObjectEditPlane.PlaceableObjectType.NoteBlock, false);
        plane.SetSelectedObjectType(ObjectEditPlane.PlaceableObjectType.Bomb);
        CheckState(ObjectEditPlane.PlaceableObjectType.Bomb, false);
        editor.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        async Task Click(Button button)
        {
            var position = button.GetGlobalRect().GetCenter();
            Check(button.GetGlobalRect().HasArea(), "Native click target has completed layout");
            using var motion = new InputEventMouseMotion { Position = position, GlobalPosition = position };
            viewport.PushInput(motion, true);
            using var press = new InputEventMouseButton
            {
                Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left,
                ButtonMask = MouseButtonMask.Left, Pressed = true,
            };
            viewport.PushInput(press, true);
            using var release = new InputEventMouseButton
            {
                Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, Pressed = false,
            };
            viewport.PushInput(release, true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var drawMode = button.GetDrawMode();
            Check(button.ButtonPressed
                ? drawMode == BaseButton.DrawMode.Pressed || drawMode == BaseButton.DrawMode.HoverPressed
                : drawMode == BaseButton.DrawMode.Normal || drawMode == BaseButton.DrawMode.Hover,
                "Native draw mode matches the synchronized state after mouse release");
        }
    }

    private void CheckSettingsToggles()
    {
        var subdivisionScene = GD.Load<PackedScene>("res://editor/ObjectEditPlane/beat_subdivision_selector_ui.tscn");
        var selector = subdivisionScene.Instantiate<BeatSubdivisionSelectorUI>();
        AddChild(selector);
        var subdivisions = new[] { 1, 2, 4, 8, 16 };
        var requests = 0;
        var selectedSubdivision = 0;
        var toggles = 0;
        selector.SubdivisionSelected += subdivision => { requests++; selectedSubdivision = subdivision; };
        var group = selector.GetNode<Button>("%BeatSubdivision1Button").ButtonGroup;
        Check(group is not null && !group.AllowUnpress, "Beat snap uses an exclusive group");
        foreach (var subdivision in subdivisions)
        {
            var button = selector.GetNode<Button>($"%BeatSubdivision{subdivision}Button");
            CheckToggleTheme(button);
            Check(button.ButtonGroup == group, "Beat snap buttons share a group");
            button.Toggled += _ => toggles++;
        }
        foreach (var selected in subdivisions)
        {
            selector.SetSelectedSubdivision(selected);
            foreach (var subdivision in subdivisions)
            {
                Check(selector.GetNode<Button>($"%BeatSubdivision{subdivision}Button").ButtonPressed
                    == (subdivision == selected), "Beat snap selection synchronizes pressed states");
            }
        }
        Check(requests == 0 && toggles == 0, "Beat snap synchronization is silent");
        selector.GetNode<Button>("%BeatSubdivision4Button").EmitSignal(Button.SignalName.Pressed);
        Check(requests == 1 && selectedSubdivision == 4, "Beat snap still emits selection requests");
        selector.Free();

        var settingsScene = GD.Load<PackedScene>("res://editor/ObjectEditPlane/spectrogram_settings_ui.tscn");
        var settings = settingsScene.Instantiate<Control>();
        AddChild(settings);
        var playbackToggle = settings.GetNode<Button>("%ShowDuringPlaybackButton");
        CheckToggleTheme(playbackToggle);
        Check(playbackToggle.ButtonGroup is null, "Playback visibility is an independent toggle");
        playbackToggle.SetPressedNoSignal(true);
        Check(playbackToggle.ButtonPressed, "Playback visibility supports native pressed state");
        settings.Free();
    }

    private void CheckToggleTheme(Button button)
    {
        var theme = GD.Load<Theme>("res://bs_mapper_theme.tres");
        Check(button.ToggleMode && button.ThemeTypeVariation == "EditorToggleButton",
            "Setting button uses toggle mode and shared theme variant");
        foreach (var style in new[] { "normal", "hover", "pressed", "hover_pressed" })
        {
            Check(!button.HasThemeStyleboxOverride(style)
                && button.GetThemeStylebox(style) == theme.GetStylebox(style, "EditorToggleButton"),
                "Setting button resolves shared style without overrides");
        }
        Check(button.GetThemeStylebox("pressed") == button.GetThemeStylebox("hover_pressed"),
            "Selected highlight remains visible while hovered");
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
