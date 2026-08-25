using System;
using Godot;

[GlobalClass]
public partial class BeatSubdivisionSelector : Node3D
{
    private const float PanelMargin = 0.95f;
    private const float PanelHeight = 0.3f;
    private const float PanelZOffset = 0.12f;

    private PlaybackManager _playbackManager;
    private BeatSubdivisionSelectorUI _selectorUi;

    public override void _Ready()
    {
        Position = new Vector3(
            -NoteBlockLane.LaneWidth / 2.0f - PanelMargin,
            GlobalSettings.PlayerHeight / 3.0f + PanelHeight / 2.0f,
            PanelZOffset);

        _playbackManager = GetNode<PlaybackManager>("/root/PlaybackManager");
        var viewportPanel = GetNode<Node>("ViewportPanel");
        _selectorUi = viewportPanel.Call("get_scene_instance").AsGodotObject() as BeatSubdivisionSelectorUI
            ?? throw new InvalidOperationException("Beat subdivision selector viewport scene failed to initialize");

        _selectorUi.SubdivisionSelected += _playbackManager.SetBeatSubdivision;
        _playbackManager.BeatSubdivisionChanged += OnBeatSubdivisionChanged;
        _selectorUi.SetSelectedSubdivision(_playbackManager.BeatSubdivision);
    }

    public override void _ExitTree()
    {
        if (_playbackManager is not null)
        {
            _playbackManager.BeatSubdivisionChanged -= OnBeatSubdivisionChanged;
        }
    }

    private void OnBeatSubdivisionChanged(int subdivision)
    {
        _selectorUi?.SetSelectedSubdivision(subdivision);
    }
}
