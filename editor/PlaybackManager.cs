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
    public event Action<bool> ShowSpectrogramDuringPlaybackChanged;
    public event Action<double, double> PlaybackProgressChanged;

    public AudioStreamPlayer Music { get; private set; }
    public BeatMapDifficultyInfo BeatmapDifficulty { get; private set; }
    public double PlaybackPosition { get; private set; }
    public double PlaybackDuration => GetMusicLength();
    public double PlaybackBeat => BeatmapDifficulty is null || BeatmapDifficulty.Bpm == 0.0f
        ? 0.0
        : PlaybackPosition / (60.0 / BeatmapDifficulty.Bpm);
    public int BeatSubdivision { get; private set; } = 1;
    public bool ShowSpectrogramDuringPlayback { get; private set; }
    public EditMode Mode { get; private set; } = EditMode.Playing;
    public bool Initialized { get; private set; }

    private float PlaybackScrubVelocity = 0.0f;
    private double _beatSnapOffset;

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
        Initialized = true;
        NotifyPlaybackProgressChanged();
    }

    public void SetBeatSubdivision(int subdivision)
    {
        if (BeatSubdivision == subdivision)
        {
            return;
        }

        BeatSubdivision = subdivision;
        _beatSnapOffset = GetPlaybackPosition();
        BeatSubdivisionChanged?.Invoke(subdivision);
    }

    public void SetShowSpectrogramDuringPlayback(bool enabled)
    {
        if (ShowSpectrogramDuringPlayback == enabled)
        {
            return;
        }

        ShowSpectrogramDuringPlayback = enabled;
        ShowSpectrogramDuringPlaybackChanged?.Invoke(enabled);
    }

    private void OnCurrentBeatMapInfoChanged(BeatMapInfo beatmap)
    {
        Music.Stream = beatmap is null
            ? null
            : AudioStreamOggVorbis.LoadFromFile(beatmap.SongFilePath);
        PlaybackPosition = 0.0;
        NotifyPlaybackProgressChanged();
    }

    public void Play(double fromPosition = 0.0)
    {
        SetPlaybackPosition(fromPosition);
        if (Mode == EditMode.Playing && Music.Stream is not null)
        {
            Music.Play((float)PlaybackPosition);
        }
    }

    public void Pause()
    {
        if (Music.Stream is not null)
        {
            Music.StreamPaused = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Initialized)
        {
            return;
        }

        if (PlaybackScrubVelocity != 0.0f)
        {
            SetPlaybackPosition(PlaybackPosition + PlaybackScrubVelocity * delta * GetMusicLength());
        }
    }

    public override void _Process(double delta)
    {
        if (!Initialized)
        {
            return;
        }

        if (Music.Stream is null || Music.StreamPaused || !Music.Playing)
        {
            return;
        }

        PlaybackPosition = Mathf.Clamp(
            Music.GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix(),
            0.0,
            GetMusicLength());
        NotifyPlaybackProgressChanged();
    }

    public double GetPlaybackPosition()
    {
        return PlaybackPosition;
    }

    public void SetPlaybackPosition(double position)
    {
        PlaybackPosition = Mathf.Clamp(position, 0.0, GetMusicLength());
        if (Music.Playing)
        {
            Music.Seek((float)PlaybackPosition);
        }
        NotifyPlaybackProgressChanged();
    }

    public void BeginScrub()
    {
        Pause();
    }

    public void EndScrub()
    {
        if (Mode == EditMode.Playing && Music.Stream is not null)
        {
            Music.Play((float)PlaybackPosition);
        }
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

    public void SnapToNearestBeat()
    {
        if (BeatmapDifficulty is null)
        {
            return;
        }

        var playbackPosition = GetPlaybackPosition();
        var subdivisionDuration = BeatmapDifficulty.BeatDuration / BeatSubdivision;
        var snappedPosition = Mathf.Round((playbackPosition - _beatSnapOffset) / subdivisionDuration)
            * subdivisionDuration
            + _beatSnapOffset;
        SetPlaybackPosition(snappedPosition);
    }

    public void StepBeatSubdivision(int direction)
    {
        if (BeatmapDifficulty is null || Music.Stream is null)
        {
            return;
        }

        var subdivisionDuration = BeatmapDifficulty.BeatDuration / BeatSubdivision;
        var playbackPosition = GetPlaybackPosition() + direction * subdivisionDuration;
        SetPlaybackPosition(playbackPosition);
    }

    public void SetPlaybackScrubVelocity(float velocity)
    {
        PlaybackScrubVelocity = velocity;
    }

    private double GetMusicLength()
    {
        return Music?.Stream?.GetLength() ?? 0.0;
    }

    private void NotifyPlaybackProgressChanged()
    {
        PlaybackProgressChanged?.Invoke(PlaybackPosition, GetMusicLength());
    }
}