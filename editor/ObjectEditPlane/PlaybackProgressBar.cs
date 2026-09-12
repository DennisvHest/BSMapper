using System;
using Godot;

[GlobalClass]
public partial class PlaybackProgressBar : Node3D
{
    private const float PanelHeight = 0.2f;
    private const float PanelZOffset = 0.12f;

    private PlaybackManager _playbackManager;
    private PlaybackProgressBarUI _progressUi;

    public override void _Ready()
    {
        Position = new Vector3(
            0.0f,
            GlobalSettings.PlayerHeight / 3.0f - PanelHeight / 2.0f - 0.08f,
            PanelZOffset);

        _playbackManager = GetNode<PlaybackManager>("/root/PlaybackManager");
        var viewportPanel = GetNode<Node>("ViewportPanel");
        _progressUi = viewportPanel.Call("get_scene_instance").AsGodotObject() as PlaybackProgressBarUI
            ?? throw new InvalidOperationException("Playback progress viewport scene failed to initialize");

        _progressUi.ScrubStarted += _playbackManager.BeginScrub;
        _progressUi.PositionScrubbed += _playbackManager.SetPlaybackPosition;
        _progressUi.ScrubEnded += _playbackManager.EndScrub;
        _playbackManager.PlaybackProgressChanged += OnPlaybackProgressChanged;
        _playbackManager.ModeChanged += OnPlaybackModeChanged;

        OnPlaybackProgressChanged(_playbackManager.PlaybackPosition, _playbackManager.PlaybackDuration);
        OnPlaybackModeChanged();
    }

    public override void _ExitTree()
    {
        if (_playbackManager is null)
        {
            return;
        }

        _playbackManager.PlaybackProgressChanged -= OnPlaybackProgressChanged;
        _playbackManager.ModeChanged -= OnPlaybackModeChanged;
    }

    private void OnPlaybackProgressChanged(double position, double duration)
    {
        _progressUi?.SetProgress(position, duration);
    }

    private void OnPlaybackModeChanged()
    {
        Visible = _playbackManager.Mode == PlaybackManager.EditMode.Editing;
    }
}
