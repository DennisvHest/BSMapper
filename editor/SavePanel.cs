using System;
using Godot;

public partial class SavePanel : Node3D
{
    private EditorSaveService _saving;
    private Label _autosave;
    private Label _status;
    private Button _save;

    public override void _Ready()
    {
        // Stack above the object selector (0.613333m high), leaving an 8cm gap.
        const float objectSelectorHeight = 0.613333f;
        const float panelHeight = 0.3f;
        const float gap = 0.08f;
        Position = new Vector3(-NoteBlockLane.LaneWidth / 2.0f - 0.95f,
            GlobalSettings.PlayerHeight / 3.0f + NoteBlockLane.LaneHeight / 2.0f
                + objectSelectorHeight / 2.0f + gap + panelHeight / 2.0f, 0.12f);
        _saving = GetNode<EditorSaveService>("../SaveService");
        var ui = GetNode<Node>("ViewportPanel").Call("get_scene_instance").AsGodotObject() as Control
            ?? throw new InvalidOperationException("Save panel viewport scene failed to initialize");
        _autosave = ui.GetNode<Label>("%LastAutosave");
        _status = ui.GetNode<Label>("%SaveStatus");
        _save = ui.GetNode<Button>("%SaveMap");
        _save.Pressed += _saving.SaveManually;
        _saving.StatusChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        _autosave.Text = _saving.LastAutosaveTime is { } time
            ? $"Last autosave: {time:HH:mm:ss}" : "Last autosave: not yet";
        _status.Text = _saving.SaveStatus;
        _save.Disabled = !_saving.CanSave;
    }

    public override void _ExitTree()
    {
        if (_saving is not null)
        {
            _saving.StatusChanged -= Refresh;
            if (GodotObject.IsInstanceValid(_save))
                _save.Pressed -= _saving.SaveManually;
        }
    }
}
