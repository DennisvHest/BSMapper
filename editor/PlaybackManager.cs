using Godot;

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
    public BeatMapDifficultyInfo Beatmap { get; private set; }
    public double PlaybackPosition { get; private set; }
    public double PlaybackBeat => Beatmap is null || Beatmap.Bpm == 0.0f
        ? 0.0
        : PlaybackPosition / (60.0 / Beatmap.Bpm);
    public EditMode Mode { get; private set; } = EditMode.Playing;
    public bool Initialized { get; private set; }

    public override void _Ready()
    {
        var beatMapManager = GetNode<BeatMapManager>("/root/BeatMapManager");
        beatMapManager.CurrentBeatmapDifficultyInfoChanged += OnCurrentBeatmapDifficultyInfoChanged;

        Music = new AudioStreamPlayer
        {
            Stream = GD.Load<AudioStream>("res://test_beatmaps/1feab (Turn It Up - abcbadq)/song.ogg"),
            VolumeDb = -10.0f,
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
        RightHand.ButtonPressed += OnRightHandButtonPressed;
        Initialized = true;
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

    private void OnCurrentBeatmapDifficultyInfoChanged(BeatMapDifficultyInfo beatmap)
    {
        Beatmap = beatmap;
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

        if (buttonName == "grip_click")
        {
            SetPlaybackPosition(GetPlaybackPosition() - Beatmap.BeatDuration);
        }
    }

    private void OnRightHandButtonPressed(string buttonName)
    {
        if (buttonName == "grip_click")
        {
            SetPlaybackPosition(GetPlaybackPosition() + Beatmap.BeatDuration);
        }
    }

    private void SnapToNearestBeat()
    {
        var playbackPosition = GetPlaybackPosition();
        var snappedPosition = Mathf.Round(playbackPosition / Beatmap.BeatDuration) * Beatmap.BeatDuration;
        SetPlaybackPosition(snappedPosition);
    }
}