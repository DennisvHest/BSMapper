using Godot;
using System.IO;

public partial class PlaybackManager : Node
{
    public enum EditMode
    {
        Playing,
        Editing,
    }

    private const float PlaybackScrubVelocity = 0.01f;

    [Signal]
    public delegate void ModeChangedEventHandler();

    public HSlider ProgressBar { get; private set; }
    public XRController3D LeftHand { get; private set; }
    public XRController3D RightHand { get; private set; }
    public AudioStreamPlayer Music { get; private set; }
    public BeatMapDifficultyInfo BeatmapDifficulty { get; private set; }
    public double PlaybackPosition { get; private set; }
    public double PlaybackBeat => BeatmapDifficulty is null || BeatmapDifficulty.Bpm == 0.0f
        ? 0.0
        : PlaybackPosition / (60.0 / BeatmapDifficulty.Bpm);
    public EditMode Mode { get; private set; } = EditMode.Playing;
    public bool Initialized { get; private set; }

    private BeatMapManager _beatMapManager;

    public override void _Ready()
    {
        _beatMapManager = GetNode<BeatMapManager>("/root/BeatMapManager");
        _beatMapManager.CurrentBeatmapInfoChanged += OnCurrentBeatMapInfoChanged;
        _beatMapManager.CurrentBeatmapDifficultyInfoChanged += (beatmapDifficulty) => BeatmapDifficulty = beatmapDifficulty;

        Music = new AudioStreamPlayer
        {
            VolumeDb = -10.0f
        };
        GetParent().CallDeferred(Node.MethodName.AddChild, Music);
    }

    public void Initialize()
    {
        ProgressBar = GetParent().GetNode<HSlider>("Editor/DebugUI/MusicProgressBar");
        ProgressBar.DragStarted += OnMusicProgressBarDragStarted;
        ProgressBar.DragEnded += OnMusicProgressBarDragEnded;

        LeftHand = GetParent().GetNode<XRController3D>("Editor/XROrigin3D/LeftHand");
        LeftHand.InputVector2Changed += OnLeftHandInputVector2Changed;
        LeftHand.ButtonPressed += OnLeftHandButtonPressed;

        RightHand = GetParent().GetNode<XRController3D>("Editor/XROrigin3D/RightHand");
        Initialized = true;
    }

    private void OnCurrentBeatMapInfoChanged(BeatMapInfo beatmap)
    {
        Music.Stream = beatmap is null
            ? null
            : AudioStreamOggVorbis.LoadFromFile(beatmap.SongFilePath);
    }

    public void Play(double fromPosition = 0.0)
    {
        PlaybackPosition = fromPosition;
        if (Mode == EditMode.Playing)
        {
            Music.Play((float)fromPosition);
        }
    }

    public void Pause()
    {
        Music.StreamPaused = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Initialized)
        {
            return;
        }

        var leftJoystickPosition = LeftHand.GetVector2("primary");
        if (leftJoystickPosition.X != 0.0f)
        {
            ProgressBar.Value += leftJoystickPosition.X * PlaybackScrubVelocity * delta;
        }
    }

    public override void _Process(double delta)
    {
        if (!Initialized)
        {
            return;
        }

        PlaybackPosition = Music.StreamPaused
            ? GetPlaybackPosition()
            : Music.GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix();
        ProgressBar.Value = PlaybackPosition / Music.Stream.GetLength();
    }

    public double GetPlaybackPosition()
    {
        return Music.Stream.GetLength() * ProgressBar.Value;
    }

    public void SetPlaybackPosition(double position)
    {
        ProgressBar.Value = position / Music.Stream.GetLength();
        Play(position);
    }

    public void ChangeMode(EditMode newMode)
    {
        if (Mode == newMode)
        {
            return;
        }

        Mode = newMode;
        if (Mode == EditMode.Playing)
        {
            Play(GetPlaybackPosition());
            GD.Print("Playback started");
        }
        else
        {
            SnapToNearestBeat();
            Pause();
            GD.Print("Playback paused");
        }

        EmitSignal(SignalName.ModeChanged);
    }

    private void OnMusicProgressBarDragStarted()
    {
        Music.StreamPaused = true;
    }

    private void OnMusicProgressBarDragEnded(bool valueChanged)
    {
        if (valueChanged)
        {
            Music.Play((float)GetPlaybackPosition());
        }
    }

    private void OnLeftHandInputVector2Changed(string name, Vector2 value)
    {
        if (name != "primary")
        {
            return;
        }

        if (value.X != 0.0f)
        {
            Pause();
        }
        else
        {
            SnapToNearestBeat();
        }
    }

    private void OnLeftHandButtonPressed(string buttonName)
    {
        if (buttonName == "ax_button")
        {
            ChangeMode(Mode == EditMode.Editing ? EditMode.Playing : EditMode.Editing);
        }
    }

    private void SnapToNearestBeat()
    {
        var playbackPosition = GetPlaybackPosition();
        var snappedPosition = Mathf.Round(playbackPosition / BeatmapDifficulty.BeatDuration) * BeatmapDifficulty.BeatDuration;
        SetPlaybackPosition(snappedPosition);
    }
}