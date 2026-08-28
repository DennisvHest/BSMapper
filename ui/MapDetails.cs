using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

public partial class MapDetails : VBoxContainer
{
    [Signal]
    public delegate void OpenMapRequestedEventHandler(string infoPath);

    public event Action MapCreated;
    public event Action MapDeleted;

    private BeatMapManager _manager;
    private BeatMapInfo _beatmapInfo;
    private string _replacementAudioPath = string.Empty;
    private bool _isPopulating;

    private TextureRect _cover;
    private LineEdit _songName;
    private LineEdit _subName;
    private LineEdit _songAuthor;
    private LineEdit _creator;
    private Label _audioFilePath;
    private LineEdit _previewStartTime;
    private LineEdit _previewDuration;
    private LineEdit _bpm;
    private LineEdit _songTimeOffset;
    private Label _error;
    private Button _createMap;
    private Button _openMap;
    private Button _deleteMap;
    private ConfirmationDialog _deleteConfirmation;
    private readonly List<DifficultyConfiguration> _difficulties = new();

    public override void _Ready()
    {
        _cover = GetNode<TextureRect>("%Cover");
        _songName = GetNode<LineEdit>("%SongName");
        _subName = GetNode<LineEdit>("%SubName");
        _songAuthor = GetNode<LineEdit>("%SongAuthor");
        _creator = GetNode<LineEdit>("%Creator");
        _audioFilePath = GetNode<Label>("%AudioFilePath");
        _previewStartTime = GetNode<LineEdit>("%PreviewStartTime");
        _previewDuration = GetNode<LineEdit>("%PreviewDuration");
        _bpm = GetNode<LineEdit>("%Bpm");
        _songTimeOffset = GetNode<LineEdit>("%SongTimeOffset");
        _error = GetNode<Label>("%Error");
        _createMap = GetNode<Button>("%CreateMap");
        _openMap = GetNode<Button>("%OpenMap");
        _deleteMap = GetNode<Button>("%DeleteMap");
        _deleteConfirmation = GetNode<ConfirmationDialog>("%DeleteConfirmation");

        foreach (var child in GetNode("%Difficulties").GetChildren())
        {
            if (child is DifficultyConfiguration difficulty)
            {
                _difficulties.Add(difficulty);
                difficulty.ConfigurationChanged += Save;
            }
        }

        foreach (var edit in GetMetadataEdits())
        {
            edit.TextSubmitted += _ => Save();
            edit.FocusExited += Save;
        }

        GetNode<Button>("%SelectAudioFile").Pressed += () => GetNode<FileDialog>("%AudioFileDialog").Show();
        GetNode<FileDialog>("%AudioFileDialog").FileSelected += OnAudioFileSelected;
        _createMap.Pressed += CreateMap;
        _deleteMap.Pressed += () => _deleteConfirmation.PopupCentered();
        _deleteConfirmation.Confirmed += DeleteMap;
        _openMap.Pressed += () =>
        {
            if (_beatmapInfo is not null)
            {
                EmitSignal(SignalName.OpenMapRequested, _beatmapInfo.FilePath);
            }
        };
    }

    public void BeginCreate(BeatMapManager manager)
    {
        _isPopulating = true;
        _manager = manager;
        _beatmapInfo = null;
        _replacementAudioPath = string.Empty;

        _songName.Clear();
        _subName.Clear();
        _songAuthor.Clear();
        _creator.Clear();
        _audioFilePath.Text = "No file selected";
        _previewStartTime.Text = "0";
        _previewDuration.Text = "0";
        _bpm.Text = "120";
        _songTimeOffset.Text = "0";
        _cover.Texture = LoadImage("res://icon.svg");
        _error.Hide();

        foreach (var difficulty in _difficulties)
        {
            difficulty.Populate(null);
        }

        _createMap.Show();
        _openMap.Hide();
        _deleteMap.Hide();
        Show();
        _isPopulating = false;
        _songName.GrabFocus();
    }

    public void Populate(BeatMapManager manager, BeatMapInfo beatmapInfo)
    {
        _isPopulating = true;
        _manager = manager;
        _beatmapInfo = beatmapInfo;
        _replacementAudioPath = string.Empty;

        _songName.Text = beatmapInfo.SongName;
        _subName.Text = beatmapInfo.SongSubName;
        _songAuthor.Text = beatmapInfo.SongAuthorName;
        _creator.Text = beatmapInfo.LevelAuthorName;
        _audioFilePath.Text = beatmapInfo.SongFilePath;
        _previewStartTime.Text = Format(beatmapInfo.PreviewStartTime);
        _previewDuration.Text = Format(beatmapInfo.PreviewDuration);
        _bpm.Text = Format(beatmapInfo.Bpm);
        _songTimeOffset.Text = Format(beatmapInfo.SongTimeOffset);
        _cover.Texture = LoadCoverImage(beatmapInfo);
        _error.Hide();

        const BeatMapDifficultySet.BeatmapMode mode = BeatMapDifficultySet.BeatmapMode.Standard;
        foreach (var difficulty in _difficulties)
        {
            difficulty.Populate(beatmapInfo.FindDifficulty(difficulty.DifficultyLevel, mode));
        }

        _createMap.Hide();
        _openMap.Show();
        _deleteMap.Show();
        Show();
        _isPopulating = false;
    }

    private void DeleteMap()
    {
        if (_manager is null || _beatmapInfo is null)
        {
            return;
        }

        try
        {
            _manager.DeleteMap(_beatmapInfo);
            _beatmapInfo = null;
            Hide();
            MapDeleted?.Invoke();
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
    }

    private void CreateMap()
    {
        if (_manager is null || string.IsNullOrWhiteSpace(_songName.Text)
            || !FileAccess.FileExists(_replacementAudioPath))
        {
            ShowError("Enter a song name and select an existing audio file.");
            return;
        }

        if (!TryReadFormValues(
                out var bpm,
                out var previewStartTime,
                out var previewDuration,
                out var songTimeOffset))
        {
            return;
        }

        var settings = GetDifficultySettings();
        try
        {
            var beatmapInfo = _manager.NewMap(
                _replacementAudioPath,
                _songName.Text.Trim(),
                _subName.Text.Trim(),
                _songAuthor.Text.Trim(),
                bpm);

            foreach (var setting in settings)
            {
                _manager.NewDifficulty(
                    beatmapInfo,
                    BeatMapDifficultySet.BeatmapMode.Standard,
                    setting.Difficulty,
                    setting.Njs,
                    setting.NoteJumpStartBeatOffset);
            }

            _manager.UpdateMap(
                beatmapInfo,
                beatmapInfo.SongName,
                beatmapInfo.SongSubName,
                beatmapInfo.SongAuthorName,
                _creator.Text.Trim(),
                bpm,
                previewStartTime,
                previewDuration,
                songTimeOffset,
                string.Empty,
                settings);

            Populate(_manager, beatmapInfo);
            MapCreated?.Invoke();
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
    }

    private void Save()
    {
        if (_isPopulating || _beatmapInfo is null || _manager is null)
        {
            return;
        }

        if (!TryReadFormValues(
                out var bpm,
                out var previewStartTime,
                out var previewDuration,
                out var songTimeOffset))
        {
            return;
        }

        var settings = GetDifficultySettings();

        try
        {
            _manager.UpdateMap(
                _beatmapInfo,
                _songName.Text,
                _subName.Text,
                _songAuthor.Text,
                _creator.Text,
                bpm,
                previewStartTime,
                previewDuration,
                songTimeOffset,
                _replacementAudioPath,
                settings);
            _replacementAudioPath = string.Empty;
            _audioFilePath.Text = _beatmapInfo.SongFilePath;
            _error.Hide();
        }
        catch (Exception exception)
        {
            _error.Text = exception.Message;
            _error.Show();
        }
    }

    private bool TryReadFormValues(
        out float bpm,
        out float previewStartTime,
        out float previewDuration,
        out float songTimeOffset)
    {
        if (TryReadFloat(_bpm, out bpm) && bpm > 0.0f
            && TryReadFloat(_previewStartTime, out previewStartTime)
            && TryReadFloat(_previewDuration, out previewDuration)
            && TryReadFloat(_songTimeOffset, out songTimeOffset))
        {
            return true;
        }

        previewStartTime = 0.0f;
        previewDuration = 0.0f;
        songTimeOffset = 0.0f;
        ShowError("BPM must be greater than zero and timing values must be valid numbers.");
        return false;
    }

    private List<BeatMapManager.DifficultySettings> GetDifficultySettings()
    {
        var settings = new List<BeatMapManager.DifficultySettings>();
        foreach (var difficulty in _difficulties)
        {
            if (difficulty.ButtonPressed)
            {
                settings.Add(difficulty.GetSettings());
            }
        }

        return settings;
    }

    private void ShowError(string message)
    {
        _error.Text = message;
        _error.Show();
    }

    private void OnAudioFileSelected(string path)
    {
        _replacementAudioPath = path;
        _audioFilePath.Text = path;
        Save();
    }

    private IEnumerable<LineEdit> GetMetadataEdits()
    {
        yield return _songName;
        yield return _subName;
        yield return _songAuthor;
        yield return _creator;
        yield return _previewStartTime;
        yield return _previewDuration;
        yield return _bpm;
        yield return _songTimeOffset;
    }

    private static bool TryReadFloat(LineEdit edit, out float value)
    {
        return float.TryParse(edit.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string Format(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static Texture2D LoadCoverImage(BeatMapInfo beatmapInfo)
    {
        var path = string.IsNullOrWhiteSpace(beatmapInfo.CoverImageFileName)
            ? "res://icon.svg"
            : beatmapInfo.MapFolder.PathJoin(beatmapInfo.CoverImageFileName);
        return LoadImage(path) ?? LoadImage("res://icon.svg");
    }

    private static Texture2D LoadImage(string path)
    {
        var image = Image.LoadFromFile(path);
        return image is null ? null : ImageTexture.CreateFromImage(image);
    }
}
