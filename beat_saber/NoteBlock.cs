using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class NoteBlock : BeatmapObject
{
    public const float RotationAnimationTime = 0.2f;

    private readonly HashSet<ulong> _hoveringPointers = new();
    private MeshInstance3D _highlightOutline;
    private Node3D _visual;
    private CollisionShape3D _collisionShape;
    private float _blockRotation;

    public override void _Ready()
    {
        EnsureNodeReferences();
        base._Ready();
        SetHighlightVisible(false);
        VisibilityChanged += OnVisibilityChanged;
    }

    public void InitializeNote(
        Vector3 initialPosition,
        BeatMapDifficultyInfo mapInfo,
        BeatMapNote note)
    {
        EnsureNodeReferences();
        Initialize(initialPosition, mapInfo, note);
        SetNoteBlockColor(note);
        SetCutDirection(note.Cut);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!Visible || Despawned)
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

        var visualRotation = _visual.GlobalRotation;
        visualRotation.Z = GetNoteVisualRotation(jumpTime);
        _visual.GlobalRotation = visualRotation;
    }

    public void DeleteNoteBlock()
    {
        DeleteBeatmapObject();
    }

    public void SetCutDirection(BeatMapNote.CutDirection cutDirection)
    {
        if (BeatmapData is BeatMapNote note)
        {
            note.Cut = cutDirection;
        }

        _blockRotation = cutDirection switch
        {
            BeatMapNote.CutDirection.Up => 180.0f,
            BeatMapNote.CutDirection.Left => -90.0f,
            BeatMapNote.CutDirection.Right => 90.0f,
            BeatMapNote.CutDirection.UpLeft => -135.0f,
            BeatMapNote.CutDirection.UpRight => 135.0f,
            BeatMapNote.CutDirection.DownLeft => -45.0f,
            BeatMapNote.CutDirection.DownRight => 45.0f,
            _ => 0.0f,
        };

        var rotation = Rotation;
        rotation.Z = Mathf.DegToRad(_blockRotation);
        Rotation = rotation;
        GetNode<Node3D>("Visual/CutDirectionTriangle").Visible = cutDirection != BeatMapNote.CutDirection.Any;
        GetNode<Node3D>("Visual/AnyCutDirectionCircle").Visible = cutDirection == BeatMapNote.CutDirection.Any;
    }

    private void SetNoteBlockColor(BeatMapNote note)
    {
        var mesh = GetNode<MeshInstance3D>("Visual/MeshInstance3D");
        if (mesh.GetActiveMaterial(0) is StandardMaterial3D material)
        {
            material.AlbedoColor = note.Type == BeatMapNote.NoteBlockType.Left ? Colors.Red : Colors.Blue;
        }
    }

    private float GetNoteVisualRotation(float jumpTime)
    {
        if (!JumpAnimationEnabled)
        {
            return Mathf.DegToRad(_blockRotation);
        }

        var jumpProgress = (jumpTime - ObjectTime) / MapInfo.ReactionTime;
        if (jumpProgress <= 0.0f)
        {
            return 0.0f;
        }

        if (jumpProgress < RotationAnimationTime)
        {
            var rotationProgress = jumpProgress / RotationAnimationTime;
            return Mathf.DegToRad(_blockRotation * Mathf.Ease(rotationProgress, 0.5f));
        }

        return Mathf.DegToRad(_blockRotation);
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

    private void EnsureNodeReferences()
    {
        _highlightOutline ??= GetNode<MeshInstance3D>("Visual/HighlightOutline");
        _visual ??= GetNode<Node3D>("Visual");
        _collisionShape ??= GetNode<CollisionShape3D>("Area3D/CollisionShape3D");
    }

    private void OnArea3DAreaEntered(Area3D area)
    {
        if (!area.IsInGroup(Groups.Sabers) || PlaybackManager.Mode == PlaybackManager.EditMode.Editing)
        {
            return;
        }

        if (area.GetParent() is not Saber saber)
        {
            throw new InvalidCastException("Expected parent to be Saber");
        }

        GetNode<GameEvents>("/root/GameEvents").EmitSignal(GameEvents.SignalName.NoteBlockHit, (int)saber.Type);
        Despawn();
    }
}