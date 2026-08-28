using Godot;

public partial class DifficultyConfiguration : CheckBox
{
    [Signal]
    public delegate void ConfigurationChangedEventHandler();

    [Export]
    public BeatMapDifficultyInfo.Difficulty DifficultyLevel { get; set; }

    [Export]
    public float DefaultNjs { get; set; } = 10.0f;

    private SpinBox _njs;
    private SpinBox _offset;
    private bool _isPopulating;

    public override void _Ready()
    {
        _njs = GetNode<SpinBox>("%Njs");
        _offset = GetNode<SpinBox>("%Offset");
        Text = BeatMapDifficultyInfo.GetDifficultyDisplayName(DifficultyLevel);
        _njs.Value = DefaultNjs;

        Toggled += OnToggled;
        _njs.ValueChanged += OnValueChanged;
        _offset.ValueChanged += OnValueChanged;
        SetInputsEnabled(ButtonPressed);
    }

    public void Populate(BeatMapDifficultyInfo difficulty)
    {
        _isPopulating = true;
        ButtonPressed = difficulty is not null;
        _njs.Value = difficulty?.Njs ?? DefaultNjs;
        _offset.Value = difficulty?.NoteJumpStartBeatOffset ?? 0.0f;
        SetInputsEnabled(ButtonPressed);
        _isPopulating = false;
    }

    public BeatMapManager.DifficultySettings GetSettings()
    {
        return new BeatMapManager.DifficultySettings(
            DifficultyLevel,
            (float)_njs.Value,
            (float)_offset.Value);
    }

    private void OnToggled(bool toggledOn)
    {
        SetInputsEnabled(toggledOn);
        NotifyChanged();
    }

    private void OnValueChanged(double value)
    {
        NotifyChanged();
    }

    private void SetInputsEnabled(bool enabled)
    {
        _njs.Editable = enabled;
        _offset.Editable = enabled;
    }

    private void NotifyChanged()
    {
        if (!_isPopulating)
        {
            EmitSignal(SignalName.ConfigurationChanged);
        }
    }
}
