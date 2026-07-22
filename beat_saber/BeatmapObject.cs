using Godot;

[GlobalClass]
public partial class BeatmapObject : Node3D
{
    public enum BeatmapObjectType
    {
        NoteBlockLeft,
        NoteBlockRight,
        Bomb = 3,
    }

    public const float SnapInAnimationTime = 0.2f;
    public const float SnapInAnimationDistance = 65.0f;

    public BeatMapDifficultyInfo MapInfo { get; private set; }
    public BeatMapObjectBase BeatmapData { get; private set; }
    public Vector3 InitialPosition { get; private set; }
    public float ObjectTime { get; private set; }
    public bool JumpAnimationEnabled { get; private set; } = true;
    public bool Despawned => ProcessMode == ProcessModeEnum.Disabled;

    protected PlaybackManager PlaybackManager => GetNode<PlaybackManager>("/root/PlaybackManager");

    public override void _Ready()
    {
        OnPlaybackModeChanged();
        PlaybackManager.ModeChanged += OnPlaybackModeChanged;
    }

    public override void _ExitTree()
    {
        PlaybackManager.ModeChanged -= OnPlaybackModeChanged;
    }

    public virtual void Initialize(
        Vector3 initialPosition,
        BeatMapDifficultyInfo mapInfo,
        BeatMapObjectBase beatmapObject)
    {
        InitialPosition = initialPosition;
        Position = initialPosition;
        MapInfo = mapInfo;
        BeatmapData = beatmapObject;
        ObjectTime = (float)(beatmapObject.Beat / mapInfo.Bpm * 60.0);
    }

    public override void _Process(double delta)
    {
        var jumpTime = GetJumpTime();
        var objectSpawnTime = jumpTime + MapInfo.ReactionTime;
        var objectDespawnTime = objectSpawnTime - MapInfo.ReactionTime * 4.0f;
        Visible = ObjectTime <= objectSpawnTime && ObjectTime >= objectDespawnTime;
    }

    public void SetJumpAnimationEnabled(bool enabled)
    {
        JumpAnimationEnabled = enabled;
    }

    public void DeleteBeatmapObject()
    {
        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        manager.CurrentBeatmap?.RemoveObject(BeatmapData);
        QueueFree();
    }

    protected void Spawn()
    {
        SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Inherit);
        CallDeferred(Node3D.MethodName.Show);
    }

    protected void Despawn()
    {
        Hide();
        SetDeferred(Node.PropertyName.ProcessMode, (int)ProcessModeEnum.Disabled);
    }

    protected float GetJumpTime()
    {
        return GetPlaybackPosition() + MapInfo.ReactionTime;
    }

    protected float GetDistance(float jumpTime)
    {
        var timeDistance = ObjectTime - GetPlaybackPosition();
        return timeDistance * MapInfo.Njs;
    }

    protected float GetVisualDistance(float jumpTime)
    {
        if (ObjectTime <= jumpTime || !JumpAnimationEnabled)
        {
            return GetDistance(jumpTime);
        }

        var timeDistance = (ObjectTime - jumpTime) / SnapInAnimationTime;
        return MapInfo.HalfJumpDistanceMeters + SnapInAnimationDistance * timeDistance;
    }

    protected float GetVisualY(float jumpTime, float distance)
    {
        if (!JumpAnimationEnabled)
        {
            return ClampVisualY(0.0f);
        }

        return ObjectTime > jumpTime ? 0.0f : ClampVisualY(distance);
    }

    private void OnPlaybackModeChanged()
    {
        if (PlaybackManager.Mode == PlaybackManager.EditMode.Editing)
        {
            Spawn();
            SetJumpAnimationEnabled(false);
        }
        else
        {
            SetJumpAnimationEnabled(true);
        }
    }

    private float GetPlaybackPosition()
    {
        return (float)PlaybackManager.PlaybackPosition;
    }

    private float ClampVisualY(float distance)
    {
        var distanceSquared = Mathf.Pow(MapInfo.HalfJumpDistanceMeters, 2.0f);
        var timeSquared = Mathf.Pow(distance, 2.0f);
        return Mathf.Clamp(
            -(InitialPosition.Y / distanceSquared) * timeSquared + InitialPosition.Y,
            -9999.0f,
            9999.0f);
    }
}