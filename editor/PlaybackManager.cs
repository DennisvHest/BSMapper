using Godot;
using System;
using System.IO;

public partial class PlaybackManager : Node
{
    public enum EditMode
    {
        Playing,
        Editing,
    }

    [Signal]
    public delegate void ModeChangedEventHandler();

    public event Action<int> BeatSubdivisionChanged;

    public HSlider ProgressBar { get; private set; }
    public AudioStreamPlayer Music { get; private set; }
    public BeatMapDifficultyInfo BeatmapDifficulty { get; private set; }
    public double PlaybackPosition { get; private set; }
    public double PlaybackBeat => BeatmapDifficulty is null || BeatmapDifficulty.Bpm == 0.0f
        ? 0.0
        : PlaybackPosition / (60.0 / BeatmapDifficulty.Bpm);
    public int BeatSubdivision { get; private set; } = 1;
    public EditMode Mode { get; private set; } = EditMode.Playing;
    public bool Initialized { get; private set; }

    private float PlaybackScrubVelocity = 0.0f;

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

        Initialized = true;
    }

    public void SetBeatSubdivision(int subdivision)
    {
        if (BeatSubdivision == subdivision)
        {
            return;
        }

        BeatSubdivision = subdivision;
        BeatSubdivisionChanged?.Invoke(subdivision);
        if (Mode == EditMode.Editing)
        {
            SnapToNearestBeat();
        }
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

        if (PlaybackScrubVelocity != 0.0f)
        {
            ProgressBar.Value += PlaybackScrubVelocity * delta;
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

    public void ToggleMode()
    {
        ChangeMode(Mode == EditMode.Editing ? EditMode.Playing : EditMode.Editing);
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

    public void SnapToNearestBeat()
    {
        var playbackPosition = GetPlaybackPosition();
        var subdivisionDuration = BeatmapDifficulty.BeatDuration / BeatSubdivision;
        var snappedPosition = Mathf.Round(playbackPosition / subdivisionDuration) * subdivisionDuration;
        SetPlaybackPosition(snappedPosition);
    }

    public void SetPlaybackScrubVelocity(float velocity)
    {
        PlaybackScrubVelocity = velocity;
    }
}