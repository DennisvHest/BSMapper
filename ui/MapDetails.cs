using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

public partial class MapDetails : VBoxContainer
{
    [Signal]
    public delegate void OpenMapRequestedEventHandler(string infoPath);

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
        GetNode<Button>("%OpenMap").Pressed += () =>
        {
            if (_beatmapInfo is not null)
            {
                EmitSignal(SignalName.OpenMapRequested, _beatmapInfo.FilePath);
            }
        };
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

        Show();
        _isPopulating = false;
    }

    private void Save()
    {
        if (_isPopulating || _beatmapInfo is null || _manager is null)
        {
            return;
        }

        if (!TryReadFloat(_bpm, out var bpm) || bpm <= 0.0f
            || !TryReadFloat(_previewStartTime, out var previewStartTime)
            || !TryReadFloat(_previewDuration, out var previewDuration)
            || !TryReadFloat(_songTimeOffset, out var songTimeOffset))
        {
            _error.Text = "BPM must be greater than zero and timing values must be valid numbers.";
            _error.Show();
            return;
        }

        var settings = new List<BeatMapManager.DifficultySettings>();
        foreach (var difficulty in _difficulties)
        {
            if (difficulty.ButtonPressed)
            {
                settings.Add(difficulty.GetSettings());
            }
        }

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
        var image = Image.LoadFromFile(path) ?? Image.LoadFromFile("res://icon.svg");
        return image is null ? null : ImageTexture.CreateFromImage(image);
    }
}
