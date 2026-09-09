using System;
using Godot;

[GlobalClass]
public partial class SpectrogramSettingsPanel : Node3D
{
    private const float PanelMargin = 0.95f;
    private const float PanelHeight = 0.3f;
    private const float PanelGap = 0.05f;
    private const float PanelZOffset = 0.12f;

    private PlaybackManager _playbackManager;
    private Node _viewportPanel;
    private Button _toggle;

    public override void _Ready()
    {
        Position = new Vector3(
            -NoteBlockLane.LaneWidth / 2.0f - PanelMargin,
            GlobalSettings.PlayerHeight / 3.0f - PanelHeight / 2.0f - PanelGap,
            PanelZOffset);

        _playbackManager = GetNode<PlaybackManager>("/root/PlaybackManager");
        _viewportPanel = GetNode<Node>("ViewportPanel");
        var panelUi = _viewportPanel.Call("get_scene_instance").AsGodotObject() as Control
            ?? throw new InvalidOperationException("Spectrogram settings viewport scene failed to initialize");
        _toggle = panelUi.GetNode<Button>("%ShowDuringPlaybackButton");
        _toggle.Toggled += _playbackManager.SetShowSpectrogramDuringPlayback;
        _playbackManager.ShowSpectrogramDuringPlaybackChanged += OnSettingChanged;
        _playbackManager.ModeChanged += OnPlaybackModeChanged;
        OnSettingChanged(_playbackManager.ShowSpectrogramDuringPlayback);
        OnPlaybackModeChanged();
    }

    private void OnSettingChanged(bool enabled)
    {
        _toggle.SetPressedNoSignal(enabled);
        _toggle.Text = enabled ? "Show during play: On" : "Show during play: Off";
    }

    private void OnPlaybackModeChanged()
    {
        var editing = _playbackManager.Mode == PlaybackManager.EditMode.Editing;
        Visible = editing;
        _viewportPanel.Call("set_enabled", editing);
        _toggle.Disabled = !editing;
    }

    public override void _ExitTree()
    {
        if (_playbackManager is not null)
        {
            _playbackManager.ShowSpectrogramDuringPlaybackChanged -= OnSettingChanged;
            _playbackManager.ModeChanged -= OnPlaybackModeChanged;
            if (GodotObject.IsInstanceValid(_toggle))
            {
                _toggle.Toggled -= _playbackManager.SetShowSpectrogramDuringPlayback;
            }
        }
    }
}
