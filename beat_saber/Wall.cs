using Godot;

[GlobalClass]
public partial class Wall : BeatmapObject
{
    private const float BeatmapObjectLineSize = 0.5f;

    private Node3D _visual;
    private float _durationInMeters;

    public override void _Ready()
    {
        _visual = GetNode<Node3D>("Visual");
        base._Ready();
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
}