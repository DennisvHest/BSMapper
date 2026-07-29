using Godot;

[GlobalClass]
public partial class ObjectEditPlane : Node3D
{
    public enum PlaceableObjectType
    {
        NoteBlock,
        Bomb,
    }

    private const float ObjectSelectorMargin = 0.95f;
    private const float ObjectSelectorZOffset = 0.12f;

    [Signal]
    public delegate void SelectedObjectTypeChangedEventHandler(PlaceableObjectType selectedObjectType);

    [Export]
    public PackedScene ObjectEditPlaneCellScene { get; set; }

    [Export]
    public PlaceableObjectType SelectedObjectType { get; private set; }

    private bool _selectionModeEnabled;

    private PlaybackManager PlaybackManager => GetNode<PlaybackManager>("/root/PlaybackManager");

    public override void _Ready()
    {
        PositionObjectTypeSelector();
        OnPlaybackModeChanged();
        SpawnGridCells();
        PlaybackManager.ModeChanged += OnPlaybackModeChanged;
    }

    public void SetSelectedObjectType(PlaceableObjectType selectedObjectType)
    {
        if (SelectedObjectType == selectedObjectType)
        {
            return;
        }

        SelectedObjectType = selectedObjectType;
        EmitSignal(SignalName.SelectedObjectTypeChanged, (int)selectedObjectType);
    }

    public void SetSelectionModeEnabled(bool enabled)
    {
        if (_selectionModeEnabled == enabled)
        {
            return;
        }

        _selectionModeEnabled = enabled;
        UpdateVisibilityAndInteraction();
    }

    private void PositionObjectTypeSelector()
    {
        GetNode<Node3D>("ObjectTypeSelector").Position = new Vector3(
            -NoteBlockLane.LaneWidth / 2.0f - ObjectSelectorMargin,
            GlobalSettings.PlayerHeight / 3.0f + NoteBlockLane.LaneHeight / 2.0f,
            ObjectSelectorZOffset);
    }

    private void SpawnGridCells()
    {
        for (var lineIndex = 0; lineIndex < NoteBlockLane.GridWidth; lineIndex++)
        {
            for (var lineLayer = 0; lineLayer < NoteBlockLane.GridHeight; lineLayer++)
            {
                var cell = ObjectEditPlaneCellScene.Instantiate<ObjectEditPlaneCell>();
                cell.SetObjectEditPlane(this);
                cell.Initialize(lineIndex, lineLayer);
                cell.Position += Vector3.Right * NoteBlockLane.BeatmapObjectLineSize * lineIndex;
                cell.Position += Vector3.Up * NoteBlockLane.BeatmapObjectLineSize * lineLayer;
                cell.Position += Vector3.Left * NoteBlockLane.LaneWidth / 2.0f;
                cell.Position += Vector3.Right * NoteBlockLane.BeatmapObjectLineSize / 2.0f;
                cell.Position += Vector3.Up * GlobalSettings.PlayerHeight / 3.0f;
                cell.Position += Vector3.Up * NoteBlockLane.BeatmapObjectLineSize / 2.0f;
                AddChild(cell);
                cell.SetInteractionEnabled(IsEditPlaneEnabled());
            }
        }
    }

    private void OnPlaybackModeChanged()
    {
        UpdateVisibilityAndInteraction();
    }

    private void UpdateVisibilityAndInteraction()
    {
        var enabled = IsEditPlaneEnabled();
        Visible = enabled;
        foreach (var child in GetChildren())
        {
            if (child is ObjectEditPlaneCell cell)
            {
                cell.SetInteractionEnabled(enabled);
            }
        }
    }

    private bool IsEditPlaneEnabled()
    {
        return PlaybackManager.Mode == PlaybackManager.EditMode.Editing && !_selectionModeEnabled;
    }
}