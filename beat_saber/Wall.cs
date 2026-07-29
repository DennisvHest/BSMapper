using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class Wall : BeatmapObject
{
    private const float BeatmapObjectLineSize = 0.5f;

    private readonly HashSet<ulong> _hoveringPointers = new();
    private Node3D _visual;
    private MeshInstance3D _highlightOutline;
    private CollisionShape3D _collisionShape;
    private float _durationInMeters;

    public override void _Ready()
    {
        _visual = GetNode<Node3D>("Visual");
        _highlightOutline = GetNode<MeshInstance3D>("Visual/HighlightOutline");
        _collisionShape = GetNode<CollisionShape3D>("Area3D/CollisionShape3D");
        base._Ready();
        UpdateHighlightVisible();
        VisibilityChanged += OnVisibilityChanged;
    }

    public void InitializeWall(
        Vector3 initialPosition,
        BeatMapDifficultyInfo mapInfo,
        BeatMapWall wall)
    {
        Initialize(initialPosition, mapInfo, wall);

        var position = Position;
        if (wall.Type == BeatMapWall.WallType.Crouch)
        {
            position.Y += 2.0f * BeatmapObjectLineSize;
        }

        Scale *= new Vector3(wall.Width, wall.Height, 1.0f);
        position.Y += wall.Height * BeatmapObjectLineSize / 2.0f - BeatmapObjectLineSize / 2.0f;
        position.X += wall.Width * BeatmapObjectLineSize / 2.0f - BeatmapObjectLineSize / 2.0f;
        Position = position;

        var durationInSeconds = (float)(wall.Duration / mapInfo.Bpm * 60.0);
        _durationInMeters = durationInSeconds * mapInfo.Njs;
        var scale = Scale;
        scale.Z *= _durationInMeters;
        Scale = scale;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!Visible)
        {
            return;
        }

        var jumpTime = GetJumpTime();
        var position = Position;
        position.Z = -GetDistance(jumpTime) - (_durationInMeters / 2.0f - 0.25f);
        Position = position;

        var visualPosition = _visual.GlobalPosition;
        visualPosition.Z = -GetVisualDistance(jumpTime) - (_durationInMeters / 2.0f - 0.25f);
        _visual.GlobalPosition = visualPosition;
    }

    protected override void OnSelectionChanged()
    {
        UpdateHighlightVisible();
    }

    private void OnVisibilityChanged()
    {
        if (!Visible)
        {
            _hoveringPointers.Clear();
        }

        UpdateHighlightVisible();
        CallDeferred(MethodName.ChangeCollisionOnVisibilityChanged);
    }

    private void ChangeCollisionOnVisibilityChanged()
    {
        _collisionShape.Disabled = !Visible;
    }

    private void OnArea3DPointerEvent(GodotObject pointerEvent)
    {
        var pointer = pointerEvent.Get("pointer").AsGodotObject();
        var pointerId = pointer.GetInstanceId();
        switch (pointerEvent.Get("event_type").AsInt32())
        {
            case 0:
                _hoveringPointers.Add(pointerId);
                break;
            case 1:
                _hoveringPointers.Remove(pointerId);
                break;
            default:
                return;
        }

        UpdateHighlightVisible();
    }

    private void UpdateHighlightVisible()
    {
        _highlightOutline.Visible = IsSelected || _hoveringPointers.Count > 0;
    }
}