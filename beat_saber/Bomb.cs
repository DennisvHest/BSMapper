using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class Bomb : BeatmapObject
{
    private readonly HashSet<ulong> _hoveringPointers = new();
    private MeshInstance3D _highlightOutline;
    private Node3D _visual;
    private CollisionShape3D _collisionShape;

    public override void _Ready()
    {
        _highlightOutline = GetNode<MeshInstance3D>("Visual/HighlightOutline");
        _visual = GetNode<Node3D>("Visual");
        _collisionShape = GetNode<CollisionShape3D>("Area3D/CollisionShape3D");
        base._Ready();
        SetHighlightVisible(false);
        VisibilityChanged += OnVisibilityChanged;
    }

    public void InitializeBomb(
        Vector3 initialPosition,
        BeatMapDifficultyInfo mapInfo,
        BeatMapBomb bomb)
    {
        Initialize(initialPosition, mapInfo, bomb);
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
        position.Z = -GetDistance(jumpTime);
        Position = position;

        var visualDistance = GetVisualDistance(jumpTime);
        var visualPosition = _visual.GlobalPosition;
        visualPosition.Z = -visualDistance;
        visualPosition.Y = GetVisualY(jumpTime, visualDistance);
        _visual.GlobalPosition = visualPosition;
    }

    private void OnVisibilityChanged()
    {
        if (!Visible)
        {
            _hoveringPointers.Clear();
            SetHighlightVisible(false);
        }

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

        SetHighlightVisible(_hoveringPointers.Count > 0);
    }

    private void SetHighlightVisible(bool visible)
    {
        _highlightOutline.Visible = visible;
    }

    private void OnArea3DAreaEntered(Area3D area)
    {
        if (!area.IsInGroup("sabers") || PlaybackManager.Get("mode").AsInt32() == 1)
        {
            return;
        }

        if (area.GetParent() is not Saber saber)
        {
            throw new InvalidCastException("Expected parent to be Saber");
        }

        GetNode<Node>("/root/GameEvents").EmitSignal("bomb_hit", (int)saber.Type);
        Despawn();
    }
}