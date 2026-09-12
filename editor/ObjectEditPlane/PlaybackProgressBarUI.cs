using System;
using Godot;

[GlobalClass]
public partial class PlaybackProgressBarUI : Control
{
    public event Action ScrubStarted;
    public event Action<double> PositionScrubbed;
    public event Action ScrubEnded;

    private HSlider _progressSlider;
    private Label _elapsedTimeLabel;
    private Label _durationLabel;
    private bool _updatingProgress;

    public override void _Ready()
    {
        _progressSlider = GetNode<HSlider>("%ProgressSlider");
        _elapsedTimeLabel = GetNode<Label>("%ElapsedTimeLabel");
        _durationLabel = GetNode<Label>("%DurationLabel");

        _progressSlider.DragStarted += () => ScrubStarted?.Invoke();
        _progressSlider.DragEnded += _ => ScrubEnded?.Invoke();
        _progressSlider.ValueChanged += OnSliderValueChanged;
        SetProgress(0.0, 0.0);
    }

    public void SetProgress(double position, double duration)
    {
        _updatingProgress = true;
        _progressSlider.MaxValue = Math.Max(duration, 0.001);
        _progressSlider.Value = Math.Clamp(position, 0.0, duration);
        _progressSlider.Editable = duration > 0.0;
        _elapsedTimeLabel.Text = FormatTime(position);
        _durationLabel.Text = FormatTime(duration);
        _updatingProgress = false;
    }

    private void OnSliderValueChanged(double value)
    {
        if (!_updatingProgress)
        {
            PositionScrubbed?.Invoke(value);
        }
    }

    private static string FormatTime(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(seconds, 0.0));
        return time.TotalHours >= 1.0
            ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes}:{time.Seconds:00}";
    }
}
