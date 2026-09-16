using System.Collections.Generic;
using Godot;

public partial class BulkSelection : Node3D
{
    private const float OutlineThickness = 0.008f;
    private readonly List<MeshInstance3D> _edges = new();
    private readonly List<BeatmapObject> _matches = new();
    private Editor _editor;
    private NoteBlockLane _lane;
    private PlaybackManager _playback;
    private Node _dragPointer;
    private Vector2I _startCell;
    private Vector2I _endCell;
    private double _anchorBeat;
    private bool _active;
    private BulkSelectionRange? _lastRange;
    private MeshInstance3D _volume;
    private MeshInstance3D _rectangle;

    public override void _Ready()
    {
        _lane = GetParent().GetParent<NoteBlockLane>();
        _editor = _lane.GetParent<Editor>();
        _playback = GetNode<PlaybackManager>("/root/PlaybackManager");
        _volume = CreateBox(CreateMaterial(new Color(0.0f, 0.9f, 1.0f, 0.12f)));
        _rectangle = CreateBox(CreateMaterial(new Color(0.0f, 0.9f, 1.0f, 0.3f)));
        var outlineMaterial = CreateMaterial(new Color(0.0f, 0.95f, 1.0f));
        for (var i = 0; i < 12; i++)
        {
            _edges.Add(CreateBox(outlineMaterial));
        }
        Hide();
    }

    public bool BeginDrag(Node pointer)
    {
        if (_dragPointer is not null || _lane.DifficultyInfo is null
            || !TryGetPlanePosition(pointer, out var position) || !IsOnGrid(position))
        {
            return false;
        }

        var ray = pointer.GetNode<RayCast3D>("RayCast");
        if (ray.IsColliding())
        {
            var target = ray.GetCollider() as Node;
            if (target is not null && target.GetParent() is not ObjectEditPlaneCell
                && target is not BeatmapObject && target.GetParent() is not BeatmapObject)
            {
                return false;
            }
        }

        Reset();
        _startCell = _endCell = GetCell(position);
        _anchorBeat = _playback.PlaybackBeat;
        _dragPointer = pointer;
        _active = true;
        UpdateSelection();
        return true;
    }

    public void EndDrag(Node pointer)
    {
        if (_dragPointer != pointer)
        {
            return;
        }

        UpdateDrag();
        _dragPointer = null;
        UpdateSelection();
    }

    public void Reset()
    {
        _active = false;
        _dragPointer = null;
        _lastRange = null;
        _matches.Clear();
        _editor?.CommitBulkSelection();
        Hide();
    }

    public override void _Process(double delta)
    {
        if (!_active)
        {
            return;
        }

        UpdateDrag();
        UpdateSelection();
    }

    private void UpdateDrag()
    {
        if (_dragPointer is not null && IsInstanceValid(_dragPointer)
            && TryGetPlanePosition(_dragPointer, out var position))
        {
            _endCell = GetCell(position);
        }
    }

    private void UpdateSelection()
    {
        var range = new BulkSelectionRange(
            _startCell.X, _startCell.Y, _endCell.X, _endCell.Y, _anchorBeat, _playback.PlaybackBeat);
        if (!_lastRange.HasValue || !_lastRange.Value.Equals(range))
        {
            _matches.Clear();
            foreach (var child in _lane.GetChildren())
            {
                if (child is BeatmapObject beatmapObject && !beatmapObject.IsQueuedForDeletion()
                    && range.Contains(beatmapObject.BeatmapData))
                {
                    _matches.Add(beatmapObject);
                }
            }
            _editor.UpdateBulkSelection(_matches);
            _lastRange = range;
        }

        UpdateVisuals(range);
        Show();
    }

    private bool TryGetPlanePosition(Node pointer, out Vector3 position)
    {
        var ray = pointer.GetNode<RayCast3D>("RayCast");
        var origin = ToLocal(ray.GlobalPosition);
        var direction = ToLocal(ray.ToGlobal(ray.TargetPosition)) - origin;
        position = default;
        if (Mathf.Abs(direction.Z) < 0.00001f)
        {
            return false;
        }

        var distance = -origin.Z / direction.Z;
        if (distance < 0.0f || distance > 1.0f)
        {
            return false;
        }

        position = origin + direction * distance;
        return true;
    }

    private static bool IsOnGrid(Vector3 position)
    {
        return position.X >= -NoteBlockLane.LaneWidth / 2.0f
            && position.X <= NoteBlockLane.LaneWidth / 2.0f
            && position.Y >= GlobalSettings.PlayerHeight / 3.0f
            && position.Y <= GlobalSettings.PlayerHeight / 3.0f + NoteBlockLane.LaneHeight;
    }

    private static Vector2I GetCell(Vector3 position)
    {
        return new Vector2I(
            Mathf.Clamp(Mathf.FloorToInt((position.X + NoteBlockLane.LaneWidth / 2.0f)
                / NoteBlockLane.BeatmapObjectLineSize), 0, NoteBlockLane.GridWidth - 1),
            Mathf.Clamp(Mathf.FloorToInt((position.Y - GlobalSettings.PlayerHeight / 3.0f)
                / NoteBlockLane.BeatmapObjectLineSize), 0, NoteBlockLane.GridHeight - 1));
    }

    private void UpdateVisuals(BulkSelectionRange range)
    {
        var difficulty = _lane.DifficultyInfo;
        var metersPerBeat = difficulty.Bpm > 0.0f ? 60.0f / difficulty.Bpm * difficulty.Njs : 0.0f;
        var anchorZ = (float)((_playback.PlaybackBeat - _anchorBeat) * metersPerBeat);
        var min = new Vector3(
            range.MinColumn * NoteBlockLane.BeatmapObjectLineSize - NoteBlockLane.LaneWidth / 2.0f,
            range.MinRow * NoteBlockLane.BeatmapObjectLineSize + GlobalSettings.PlayerHeight / 3.0f,
            Mathf.Min(0.0f, anchorZ));
        var max = new Vector3(
            (range.MaxColumn + 1) * NoteBlockLane.BeatmapObjectLineSize - NoteBlockLane.LaneWidth / 2.0f,
            (range.MaxRow + 1) * NoteBlockLane.BeatmapObjectLineSize + GlobalSettings.PlayerHeight / 3.0f,
            Mathf.Max(0.0f, anchorZ));
        SetBox(_volume, (min + max) / 2.0f,
            new Vector3(max.X - min.X, max.Y - min.Y, Mathf.Max(max.Z - min.Z, 0.002f)));
        SetBox(_rectangle, new Vector3((min.X + max.X) / 2.0f, (min.Y + max.Y) / 2.0f, 0.03f),
            new Vector3(max.X - min.X, max.Y - min.Y, 0.002f));
        var edge = 0;
        for (var axis = 0; axis < 3; axis++)
        {
            var otherAxis = (axis + 1) % 3;
            var thirdAxis = (axis + 2) % 3;
            for (var side = 0; side < 4; side++)
            {
                var center = (min + max) / 2.0f;
                center[otherAxis] = (side & 1) == 0 ? min[otherAxis] : max[otherAxis];
                center[thirdAxis] = (side & 2) == 0 ? min[thirdAxis] : max[thirdAxis];
                var size = Vector3.One * OutlineThickness;
                size[axis] = Mathf.Max(max[axis] - min[axis], OutlineThickness);
                SetBox(_edges[edge++], center, size);
            }
        }
    }

    private MeshInstance3D CreateBox(StandardMaterial3D material)
    {
        var instance = new MeshInstance3D
        {
            Mesh = new BoxMesh(),
            MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(instance);
        return instance;
    }

    private static StandardMaterial3D CreateMaterial(Color color)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = color.A < 1.0f ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
    }

    private static void SetBox(MeshInstance3D instance, Vector3 center, Vector3 size)
    {
        instance.Position = center;
        ((BoxMesh)instance.Mesh).Size = size;
    }
}
