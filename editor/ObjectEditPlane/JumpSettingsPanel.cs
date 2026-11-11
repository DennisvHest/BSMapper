using System;
using System.Globalization;
using Godot;

public partial class JumpSettingsPanel : Node3D
{
    private BeatMapManager _manager;
    private PlaybackManager _playback;
    private Node _viewport;
    private Node3D _keyboard;
    private LineEdit _njs;
    private LineEdit _offset;
    private Button _njsMinus;
    private Button _njsPlus;
    private Button _offsetMinus;
    private Button _offsetPlus;

    public override void _Ready()
    {
        Position = new Vector3(NoteBlockLane.LaneWidth / 2.0f + 0.95f,
            GlobalSettings.PlayerHeight / 3.0f - 0.05f, 0.12f);
        _manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        _playback = GetNode<PlaybackManager>("/root/PlaybackManager");
        _viewport = GetNode<Node>("ViewportPanel");
        var ui = _viewport.Call("get_scene_instance").AsGodotObject() as Control
            ?? throw new InvalidOperationException("Jump settings viewport scene failed to initialize");
        _keyboard = GetNode<Node3D>("Keyboard");
        var keyboardUi = _keyboard.Call("get_scene_instance").AsGodotObject() as Node
            ?? throw new InvalidOperationException("Numeric keyboard viewport scene failed to initialize");
        foreach (var node in keyboardUi.FindChildren("*", "Button"))
        {
            var button = (Button)node;
            var key = button.GetMeta("key").AsString();
            var unicode = button.GetMeta("unicode").AsInt32();
            button.Pressed += () => keyboardUi.Call("on_key_pressed", key, unicode, false);
        }
        _njs = ui.GetNode<LineEdit>("%NjsValue");
        _offset = ui.GetNode<LineEdit>("%OffsetValue");
        _njs.FocusEntered += () => ShowKeyboard(_njs);
        _offset.FocusEntered += () => ShowKeyboard(_offset);
        _njs.TextSubmitted += _ => _njs.ReleaseFocus();
        _offset.TextSubmitted += _ => _offset.ReleaseFocus();
        _njs.FocusExited += () => CommitNumber(_njs, true);
        _offset.FocusExited += () => CommitNumber(_offset, false);
        _njsMinus = ui.GetNode<Button>("%NjsMinus");
        _njsPlus = ui.GetNode<Button>("%NjsPlus");
        _offsetMinus = ui.GetNode<Button>("%OffsetMinus");
        _offsetPlus = ui.GetNode<Button>("%OffsetPlus");
        _njsMinus.Pressed += () => Adjust(-0.1f, 0.0f);
        _njsPlus.Pressed += () => Adjust(0.1f, 0.0f);
        _offsetMinus.Pressed += () => Adjust(0.0f, -0.01f);
        _offsetPlus.Pressed += () => Adjust(0.0f, 0.01f);
        _manager.CurrentBeatmapDifficultyInfoChanged += Refresh;
        _playback.ModeChanged += OnModeChanged;
        Refresh(_manager.CurrentBeatmapDifficultyInfo);
        OnModeChanged();
    }

    private void Adjust(float speedDelta, float offsetDelta)
    {
        _njs.ReleaseFocus();
        _offset.ReleaseFocus();
        var difficulty = _manager.CurrentBeatmapDifficultyInfo;
        if (difficulty is null || _playback.Mode != PlaybackManager.EditMode.Editing)
            return;
        var speed = speedDelta == 0.0f ? difficulty.Njs
            : Mathf.Max(0.1f, Mathf.Round((difficulty.Njs + speedDelta) * 10.0f) / 10.0f);
        var offset = offsetDelta == 0.0f ? difficulty.NoteJumpStartBeatOffset
            : Mathf.Round((difficulty.NoteJumpStartBeatOffset + offsetDelta) * 100.0f) / 100.0f;
        _manager.SetCurrentJumpSettings(speed, offset);
    }

    private void Refresh(BeatMapDifficultyInfo difficulty)
    {
        if (!_njs.HasFocus())
            _njs.Text = difficulty is null ? "—" : difficulty.Njs.ToString("0.0###", CultureInfo.InvariantCulture);
        if (!_offset.HasFocus())
            _offset.Text = difficulty is null ? "—" : difficulty.NoteJumpStartBeatOffset.ToString("0.00###", CultureInfo.InvariantCulture);
        _njs.Editable = _offset.Editable = difficulty is not null;
        _njsMinus.Disabled = difficulty is null || difficulty.Njs <= 0.1f;
        _njsPlus.Disabled = _offsetMinus.Disabled = _offsetPlus.Disabled = difficulty is null;
    }

    private void OnModeChanged()
    {
        Visible = _playback.Mode == PlaybackManager.EditMode.Editing;
        _viewport.Call("set_enabled", Visible);
        if (!Visible)
        {
            _njs.ReleaseFocus();
            _offset.ReleaseFocus();
            SetKeyboardVisible(false);
            Refresh(_manager.CurrentBeatmapDifficultyInfo);
        }
    }

    private void ShowKeyboard(LineEdit field)
    {
        if (_playback.Mode != PlaybackManager.EditMode.Editing || !field.Editable)
            return;
        field.SelectAll();
        SetKeyboardVisible(true);
    }

    private void SetKeyboardVisible(bool visible)
    {
        _keyboard.Visible = visible;
        _keyboard.Call("set_enabled", visible);
    }

    private void CommitNumber(LineEdit field, bool isNjs)
    {
        var difficulty = _manager.CurrentBeatmapDifficultyInfo;
        // Accept either decimal separator; the supplied XR keyboard types a period.
        if (_playback.Mode == PlaybackManager.EditMode.Editing && difficulty is not null
            && float.TryParse(field.Text.Replace(',', '.'), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var value) && float.IsFinite(value)
            && (!isNjs || value >= 0.1f))
        {
            _manager.SetCurrentJumpSettings(isNjs ? value : difficulty.Njs,
                isNjs ? difficulty.NoteJumpStartBeatOffset : value);
        }
        Refresh(difficulty);
        SetKeyboardVisible(Visible && (_njs.HasFocus() || _offset.HasFocus()));
    }

    public override void _ExitTree()
    {
        if (_manager is not null)
            _manager.CurrentBeatmapDifficultyInfoChanged -= Refresh;
        if (_playback is not null)
            _playback.ModeChanged -= OnModeChanged;
    }
}
