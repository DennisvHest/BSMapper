using System;
using Godot;

[GlobalClass]
public partial class SelectionPanel : Node3D
{
    private const float PanelMargin = 0.95f;
    private const float PanelZOffset = 0.12f;

    private Editor _editor;
    private SelectionPanelUI _panelUi;

    public override void _Ready()
    {
        Position = new Vector3(
            NoteBlockLane.LaneWidth / 2.0f + PanelMargin,
            GlobalSettings.PlayerHeight / 3.0f + NoteBlockLane.LaneHeight / 2.0f,
            PanelZOffset);

        _editor = GetNode<Editor>("../..")
            ?? throw new InvalidOperationException("SelectionPanel must be below an Editor node");
        var viewportPanel = GetNode<Node>("ViewportPanel");
        _panelUi = viewportPanel.Call("get_scene_instance").AsGodotObject() as SelectionPanelUI
            ?? throw new InvalidOperationException("SelectionPanel viewport scene failed to initialize");

        _panelUi.NoteTypeSelected += _editor.SetSelectedNoteBlockType;
        _panelUi.DeselectAll += _editor.DeselectAllObjects;
        _panelUi.DeleteSelected += _editor.DeleteSelectedObjects;
        _editor.SelectionChanged += OnSelectionChanged;
        OnSelectionChanged(0, false);
    }

    public override void _ExitTree()
    {
        if (_editor is not null)
        {
            _editor.SelectionChanged -= OnSelectionChanged;
        }
    }

    private void OnSelectionChanged(int selectedCount, bool containsNotes)
    {
        Visible = selectedCount > 0;
        _panelUi?.SetSelection(selectedCount, containsNotes);
    }
}
