using System;
using Godot;

public partial class BulkSelectionChecks : Node
{
    private int _checks;

    public override void _Ready()
    {
        Callable.From(Run).CallDeferred();
    }

    private void Run()
    {
        try
        {
            CheckRanges();
            CheckAdditiveSelection();
            CheckSelector();
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
        var requests = 0;
        selector.BulkSelectionToggled += () => requests++;
        Check(button.Text == "Bulk select: OFF", "Bulk mode initially off");
        button.EmitSignal(Button.SignalName.Pressed);
        Check(requests == 1, "VR button emits toggle request");
        selector.SetBulkSelectionEnabled(true);
        Check(button.Text == "Bulk select: ON", "Enabled label");
        Check(button.GetThemeStylebox("normal") == selector.SelectedButtonStyle, "Enabled highlight");
        Check(selector.GetNode<Button>("%NoteButton").GetThemeStylebox("normal") == selector.IdleButtonStyle,
            "Placement highlight suppressed in bulk mode");
        selector.SetBulkSelectionEnabled(false);
        Check(button.Text == "Bulk select: OFF", "Disabled label");
        Check(selector.GetNode<Button>("%NoteButton").GetThemeStylebox("normal") == selector.SelectedButtonStyle,
            "Placement highlight restored");
        selector.Free();
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
