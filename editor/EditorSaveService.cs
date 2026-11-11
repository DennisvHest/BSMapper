using System;
using Godot;

/// <summary>Saving is active only while the editor scene is open.</summary>
public partial class EditorSaveService : Node
{
    public const double AutosaveIntervalSeconds = 30;

    public event Action StatusChanged;
    public DateTime? LastAutosaveTime { get; private set; }
    public string SaveStatus { get; private set; } = "Autosave every 30 seconds";
    public bool CanSave => _maps?.CurrentBeatmap is not null
        && !string.IsNullOrWhiteSpace(_maps.CurrentBeatmapFilePath);

    private BeatMapManager _maps;
    private Timer _timer;

    public override void _Ready()
    {
        _maps = GetNode<BeatMapManager>("/root/BeatMapManager");
        _maps.CurrentBeatmapChanged += OnMapChanged;
        _timer = new Timer { WaitTime = AutosaveIntervalSeconds, IgnoreTimeScale = true };
        AddChild(_timer);
        _timer.Timeout += Autosave;
        OnMapChanged(_maps.CurrentBeatmap);
    }

    public override void _ExitTree()
    {
        _maps.CurrentBeatmapChanged -= OnMapChanged;
        _timer.Timeout -= Autosave;
    }

    private void OnMapChanged(BeatMap map)
    {
        _timer.Stop();
        LastAutosaveTime = null;
        SaveStatus = CanSave ? "Autosave every 30 seconds" : "No map loaded";
        if (CanSave)
            _timer.Start();
        StatusChanged?.Invoke();
    }

    public void SaveManually() => Save(false);

    private void Autosave() => Save(true);

    private void Save(bool automatic)
    {
        if (!CanSave)
            return;

        try
        {
            _maps.SaveBeatmap();
            var now = DateTime.Now;
            if (automatic)
                LastAutosaveTime = now;
            SaveStatus = automatic ? "Autosave every 30 seconds" : $"Map saved at {now:HH:mm:ss}";
        }
        catch (Exception exception)
        {
            SaveStatus = automatic ? "Autosave failed - retrying in 30s" : "Save failed - please try again";
            GD.PushError($"{SaveStatus}: {exception.Message}");
        }
        StatusChanged?.Invoke();
    }
}
