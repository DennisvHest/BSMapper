using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class ObjectEditPlaneCell : Node3D
{
    private const float DirectionDragThreshold = 0.12f;
    private const float PlanePositionTolerance = 0.15f;
    private static readonly Color LeftNotePreviewColor = new(1.0f, 0.0f, 0.0f, 0.6f);
    private static readonly Color RightNotePreviewColor = new(0.0f, 0.0f, 1.0f, 0.6f);
    private static readonly Color BombPreviewColor = new(0.12f, 0.12f, 0.12f, 0.75f);

    private readonly Dictionary<ulong, DragState> _activePointerDrags = new();
    private int _lineIndex = 1;
    private int _lineLayer = 2;
    private ObjectEditPlane _objectEditPlane;
    private BeatMap _currentBeatmap;
    private MeshInstance3D _notePreviewMesh;
    private MeshInstance3D _bombPreviewMesh;
    private StandardMaterial3D _notePreviewMaterial;
    private StandardMaterial3D _bombPreviewMaterial;
    private Node3D _preview;

    private PlaybackManager PlaybackManager => GetNode<PlaybackManager>("/root/PlaybackManager");

    public int LineIndex => _lineIndex;
    public int LineLayer => _lineLayer;

    public void SetObjectEditPlane(ObjectEditPlane objectEditPlane)
    {
        _objectEditPlane = objectEditPlane;
    }

    public void Initialize(int lineIndex, int lineLayer)
    {
        _lineIndex = lineIndex;
        _lineLayer = lineLayer;
    }

    public void SetInteractionEnabled(bool enabled)
    {
        GetNode<CollisionShape3D>("EditArea/CollisionShape3D").Disabled = !enabled;
        if (enabled)
        {
            return;
        }

        _activePointerDrags.Clear();
        _preview?.Hide();
    }

    public override void _Ready()
    {
        _notePreviewMesh = GetNode<MeshInstance3D>("Preview/NotePreview");
        _bombPreviewMesh = GetNode<MeshInstance3D>("Preview/BombPreview");
        _preview = GetNode<Node3D>("Preview");
        _notePreviewMaterial = (StandardMaterial3D)_notePreviewMesh.GetActiveMaterial(0).Duplicate();
        _notePreviewMesh.SetSurfaceOverrideMaterial(0, _notePreviewMaterial);
        _bombPreviewMaterial = (StandardMaterial3D)_bombPreviewMesh.GetActiveMaterial(0).Duplicate();
        _bombPreviewMesh.SetSurfaceOverrideMaterial(0, _bombPreviewMaterial);

        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        OnBeatmapChanged(manager.CurrentBeatmap);
        manager.CurrentBeatmapChanged += OnBeatmapChanged;
    }

    private void OnEditAreaPointerEvent(GodotObject pointerEvent)
    {
        var pointer = pointerEvent.Get("pointer").AsGodotObject() as Node;
        if (pointer is null)
        {
            return;
        }

        var pointerId = pointer.GetInstanceId();
        var eventType = pointerEvent.Get("event_type").AsInt32();
        var eventPosition = pointerEvent.Get("position").AsVector3();
        switch (eventType)
        {
            case 2:
                var localPressPosition = ToLocal(eventPosition);
                var noteType = GetNoteTypeForPointer(pointer);
                var objectType = _objectEditPlane?.SelectedObjectType ?? ObjectEditPlane.PlaceableObjectType.NoteBlock;
                var cutDirection = objectType == ObjectEditPlane.PlaceableObjectType.AnyDirectionNoteBlock
                    ? BeatMapNote.CutDirection.Any
                    : BeatMapNote.CutDirection.Down;
                _activePointerDrags[pointerId] = new DragState(
                    localPressPosition,
                    cutDirection,
                    PlaybackManager.PlaybackBeat,
                    noteType,
                    objectType);
                ShowPreview(objectType, cutDirection, noteType);
                break;
            case 4:
                if (!_activePointerDrags.TryGetValue(pointerId, out var moveState))
                {
                    return;
                }

                var localMovePosition = ToLocal(eventPosition);
                if (moveState.ObjectType == ObjectEditPlane.PlaceableObjectType.NoteBlock
                    && IsPositionOnEditPlane(localMovePosition))
                {
                    moveState.CutDirection = GetCutDirectionFromDrag(moveState.StartPosition, localMovePosition);
                }

                ShowPreview(moveState.ObjectType, moveState.CutDirection, moveState.NoteType);
                break;
            case 3:
                if (!_activePointerDrags.Remove(pointerId, out var releaseState))
                {
                    return;
                }

                _preview.Hide();
                if (_currentBeatmap is null)
                {
                    return;
                }

                if (releaseState.ObjectType == ObjectEditPlane.PlaceableObjectType.Bomb)
                {
                    _currentBeatmap.AddObject(new BeatMapBomb
                    {
                        Beat = releaseState.Beat,
                        LineIndex = _lineIndex,
                        LineLayer = _lineLayer,
                    });
                    return;
                }

                _currentBeatmap.AddObject(new BeatMapNote
                {
                    Beat = releaseState.Beat,
                    LineIndex = _lineIndex,
                    LineLayer = _lineLayer,
                    Type = releaseState.NoteType,
                    Cut = releaseState.CutDirection,
                });
                break;
        }
    }

    private void OnBeatmapChanged(BeatMap beatmap)
    {
        _currentBeatmap = beatmap;
    }

    private static BeatMapNote.NoteBlockType GetNoteTypeForPointer(Node pointer)
    {
        for (var current = pointer; current is not null; current = current.GetParent())
        {
            if (current.Name == "LeftHand")
            {
                return BeatMapNote.NoteBlockType.Left;
            }

            if (current.Name == "RightHand")
            {
                return BeatMapNote.NoteBlockType.Right;
            }
        }

        return BeatMapNote.NoteBlockType.Left;
    }

    private static BeatMapNote.CutDirection GetCutDirectionFromDrag(
        Vector3 startPosition,
        Vector3 currentPosition)
    {
        var dragVector = new Vector2(
            currentPosition.X - startPosition.X,
            startPosition.Z - currentPosition.Z);
        if (dragVector.Length() < DirectionDragThreshold)
        {
            return BeatMapNote.CutDirection.Down;
        }

        var dragAngle = Mathf.RadToDeg(Mathf.Atan2(dragVector.Y, dragVector.X));
        return dragAngle switch
        {
            >= -22.5f and < 22.5f => BeatMapNote.CutDirection.Right,
            >= 22.5f and < 67.5f => BeatMapNote.CutDirection.UpRight,
            >= 67.5f and < 112.5f => BeatMapNote.CutDirection.Up,
            >= 112.5f and < 157.5f => BeatMapNote.CutDirection.UpLeft,
            >= -67.5f and < -22.5f => BeatMapNote.CutDirection.DownRight,
            >= -112.5f and < -67.5f => BeatMapNote.CutDirection.Down,
            >= -157.5f and < -112.5f => BeatMapNote.CutDirection.DownLeft,
            _ => BeatMapNote.CutDirection.Left,
        };
    }

    private static bool IsPositionOnEditPlane(Vector3 localPosition)
    {
        return Mathf.Abs(localPosition.Y) <= PlanePositionTolerance;
    }

    private void ShowPreview(
        ObjectEditPlane.PlaceableObjectType objectType,
        BeatMapNote.CutDirection cutDirection,
        BeatMapNote.NoteBlockType noteType)
    {
        _preview.Show();
        if (objectType == ObjectEditPlane.PlaceableObjectType.Bomb)
        {
            _bombPreviewMaterial.AlbedoColor = BombPreviewColor;
            var rotation = _preview.Rotation;
            rotation.Z = 0.0f;
            _preview.Rotation = rotation;
            _notePreviewMesh.Visible = false;
            _bombPreviewMesh.Visible = true;
            GetNode<Node3D>("Preview/CutDirectionTriangle").Visible = false;
            GetNode<Node3D>("Preview/AnyCutDirectionCircle").Visible = false;
            return;
        }

        _notePreviewMaterial.AlbedoColor = noteType == BeatMapNote.NoteBlockType.Right
            ? RightNotePreviewColor
            : LeftNotePreviewColor;
        var previewRotation = _preview.Rotation;
        previewRotation.Z = Mathf.DegToRad(GetCutDirectionRotation(cutDirection));
        _preview.Rotation = previewRotation;
        _notePreviewMesh.Visible = true;
        _bombPreviewMesh.Visible = false;
        GetNode<Node3D>("Preview/CutDirectionTriangle").Visible = cutDirection != BeatMapNote.CutDirection.Any;
        GetNode<Node3D>("Preview/AnyCutDirectionCircle").Visible = cutDirection == BeatMapNote.CutDirection.Any;
    }

    private static float GetCutDirectionRotation(BeatMapNote.CutDirection cutDirection)
    {
        return cutDirection switch
        {
            BeatMapNote.CutDirection.Up => 180.0f,
            BeatMapNote.CutDirection.Left => 90.0f,
            BeatMapNote.CutDirection.Right => -90.0f,
            BeatMapNote.CutDirection.UpLeft => 135.0f,
            BeatMapNote.CutDirection.UpRight => -135.0f,
            BeatMapNote.CutDirection.DownLeft => 45.0f,
            BeatMapNote.CutDirection.DownRight => -45.0f,
            _ => 0.0f,
        };
    }

    private sealed class DragState
    {
        public DragState(
            Vector3 startPosition,
            BeatMapNote.CutDirection cutDirection,
            double beat,
            BeatMapNote.NoteBlockType noteType,
            ObjectEditPlane.PlaceableObjectType objectType)
        {
            StartPosition = startPosition;
            CutDirection = cutDirection;
            Beat = beat;
            NoteType = noteType;
            ObjectType = objectType;
        }

        public Vector3 StartPosition { get; }
        public BeatMapNote.CutDirection CutDirection { get; set; }
        public double Beat { get; }
        public BeatMapNote.NoteBlockType NoteType { get; }
        public ObjectEditPlane.PlaceableObjectType ObjectType { get; }
    }
}