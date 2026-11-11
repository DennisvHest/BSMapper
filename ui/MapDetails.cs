using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public partial class MapDetails : VBoxContainer
{
    [Signal]
    public delegate void OpenMapRequestedEventHandler(string infoPath);

    public event Action MapCreated;
    public event Action MapDeleted;
    public event Action CoverChanged;

    private BeatMapManager _manager;
    private BeatMapInfo _beatmapInfo;
    private string _replacementAudioPath = string.Empty;
    private string _replacementCoverPath = string.Empty;
    private bool _isPopulating;
    private string[] _savedMetadata = Array.Empty<string>();
    private List<BeatMapManager.DifficultySettings> _savedDifficulties = new();

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
    private Button _saveMap;
    private Label _difficultyDeletionWarning;
    private Button _openMap;
    private Button _deleteMap;
    private ConfirmationDialog _deleteConfirmation;
    private ConfirmationDialog _unsavedChangesDialog;
    private Action _pendingNavigation;

    public bool HasUnsavedChanges { get; private set; }

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
        _saveMap = GetNode<Button>("%SaveMap");
        _difficultyDeletionWarning = GetNode<Label>("%DifficultyDeletionWarning");
        _openMap = GetNode<Button>("%OpenMap");
        _deleteMap = GetNode<Button>("%DeleteMap");
        _deleteConfirmation = GetNode<ConfirmationDialog>("%DeleteConfirmation");
        _unsavedChangesDialog = GetNode<ConfirmationDialog>("%UnsavedChangesDialog");
        _unsavedChangesDialog.AddButton("Discard changes", false, "discard");
        _unsavedChangesDialog.Confirmed += () =>
        {
            var navigation = _pendingNavigation;
            CancelNavigation();
            if (_beatmapInfo is null ? TryCreateMap() : TrySave())
            {
                navigation?.Invoke();
            }
        };
        _unsavedChangesDialog.CustomAction += action =>
        {
            if (action != "discard")
                return;
            var navigation = _pendingNavigation;
            CancelNavigation();
            if (_beatmapInfo is not null)
                PopulateForm(_manager, _manager.ReadBeatmapInfo(_beatmapInfo.FilePath));
            else
                BeginCreateForm(_manager);
            navigation?.Invoke();
        };
        _unsavedChangesDialog.Canceled += CancelNavigation;

        foreach (var child in GetNode("%Difficulties").GetChildren())
        {
            if (child is DifficultyConfiguration difficulty)
            {
                _difficulties.Add(difficulty);
                difficulty.ConfigurationChanged += UpdatePendingChanges;
            }
        }

        foreach (var edit in GetMetadataEdits())
        {
            edit.TextChanged += _ => UpdatePendingChanges();
        }

        GetNode<Button>("%SelectAudioFile").Pressed += () => GetNode<FileDialog>("%AudioFileDialog").Show();
        GetNode<FileDialog>("%AudioFileDialog").FileSelected += OnAudioFileSelected;
        var coverButton = GetNode<Button>("%CoverButton");
        coverButton.Pressed += () => GetNode<FileDialog>("%CoverImageFileDialog").Show();
        coverButton.MouseEntered += () => _cover.Modulate = new Color(1.2f, 1.2f, 1.2f);
        coverButton.MouseExited += () => _cover.Modulate = Colors.White;
        GetNode<FileDialog>("%CoverImageFileDialog").FileSelected += OnCoverImageSelected;
        _createMap.Pressed += () => TryCreateMap();
        _saveMap.Pressed += () => TrySave();
        _deleteMap.Pressed += () => _deleteConfirmation.PopupCentered();
        _deleteConfirmation.Confirmed += DeleteMap;
        _openMap.Pressed += () =>
        {
            if (_beatmapInfo is not null)
            {
                RequestNavigation(() => EmitSignal(SignalName.OpenMapRequested, _beatmapInfo.FilePath));
            }
        };
    }

    public void BeginCreate(BeatMapManager manager)
    {
        RequestNavigation(() => BeginCreateForm(manager));
    }

    private void RequestNavigation(Action navigation)
    {
        if (!IsVisibleInTree() || !HasUnsavedChanges)
        {
            navigation();
            return;
        }

        if (_pendingNavigation is not null)
            return;
        _pendingNavigation = navigation;
        _unsavedChangesDialog.DialogText = "You have unsaved map details. Save your changes before continuing, discard them, or cancel to keep editing."
            + (_difficultyDeletionWarning.Visible ? "\n\n" + _difficultyDeletionWarning.Text : string.Empty);
        _unsavedChangesDialog.PopupCentered();
    }

    private void CancelNavigation()
    {
        _pendingNavigation = null;
        _unsavedChangesDialog.Hide();
    }

    private void BeginCreateForm(BeatMapManager manager)
    {
        _isPopulating = true;
        _manager = manager;
        _beatmapInfo = null;
        _replacementAudioPath = string.Empty;
        _replacementCoverPath = string.Empty;

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
        _saveMap.Hide();
        _difficultyDeletionWarning.Hide();
        _openMap.Hide();
        _deleteMap.Hide();
        Show();
        _isPopulating = false;
        CaptureSavedValues();
        _songName.GrabFocus();
    }

    public void Populate(BeatMapManager manager, BeatMapInfo beatmapInfo)
    {
        RequestNavigation(() => PopulateForm(manager, manager.ReadBeatmapInfo(beatmapInfo.FilePath)));
    }

    private void PopulateForm(BeatMapManager manager, BeatMapInfo beatmapInfo)
    {
        _isPopulating = true;
        _manager = manager;
        _beatmapInfo = beatmapInfo;
        _replacementAudioPath = string.Empty;
        _replacementCoverPath = string.Empty;

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
        CaptureSavedValues();
    }

    private void CaptureSavedValues()
    {
        _savedMetadata = GetMetadataEdits().Select(edit => edit.Text).ToArray();
        _savedDifficulties = GetDifficultySettings();
        UpdatePendingChanges();
    }

    private void UpdatePendingChanges()
    {
        if (_isPopulating)
        {
            return;
        }

        HasUnsavedChanges = !GetMetadataEdits().Select(edit => edit.Text).SequenceEqual(_savedMetadata)
            || !GetDifficultySettings().SequenceEqual(_savedDifficulties)
            || !string.IsNullOrEmpty(_replacementAudioPath)
            || !string.IsNullOrEmpty(_replacementCoverPath);
        _saveMap.Visible = _beatmapInfo is not null && HasUnsavedChanges;

        var removed = _difficulties
            .Where(difficulty => !difficulty.ButtonPressed
                && _savedDifficulties.Any(saved => saved.Difficulty == difficulty.DifficultyLevel))
            .Select(difficulty => BeatMapDifficultyInfo.GetDifficultyDisplayName(difficulty.DifficultyLevel))
            .ToArray();
        _difficultyDeletionWarning.Visible = _beatmapInfo is not null && removed.Length > 0;
        _difficultyDeletionWarning.Text = $"Warning: Saving will permanently delete these difficulties and all of their notes, bombs, walls, and other data: {string.Join(", ", removed)}. Recheck a difficulty to keep it.";
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

    private bool TryCreateMap()
    {
        if (_manager is null || string.IsNullOrWhiteSpace(_songName.Text)
            || !FileAccess.FileExists(_replacementAudioPath))
        {
            ShowError("Enter a song name and select an existing audio file.");
            return false;
        }

        if (!TryReadFormValues(
                out var bpm,
                out var previewStartTime,
                out var previewDuration,
                out var songTimeOffset))
        {
            return false;
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

            if (!string.IsNullOrEmpty(_replacementCoverPath))
            {
                _manager.ReplaceCoverImage(beatmapInfo, _replacementCoverPath);
            }

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

            PopulateForm(_manager, beatmapInfo);
            MapCreated?.Invoke();
            return true;
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
            return false;
        }
    }

    private bool TrySave()
    {
        if (_isPopulating || _beatmapInfo is null || _manager is null)
        {
            return false;
        }

        if (!TryReadFormValues(
                out var bpm,
                out var previewStartTime,
                out var previewDuration,
                out var songTimeOffset))
        {
            return false;
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
            var coverChanged = !string.IsNullOrEmpty(_replacementCoverPath);
            if (coverChanged)
            {
                _manager.ReplaceCoverImage(_beatmapInfo, _replacementCoverPath);
                _replacementCoverPath = string.Empty;
            }
            _audioFilePath.Text = _beatmapInfo.SongFilePath;
            _error.Hide();
            CaptureSavedValues();
            if (coverChanged)
            {
                CoverChanged?.Invoke();
            }
            return true;
        }
        catch (Exception exception)
        {
            _error.Text = exception.Message;
            _error.Show();
            return false;
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
        UpdatePendingChanges();
    }

    private void OnCoverImageSelected(string path)
    {
        var texture = LoadImage(path);
        if (texture is null)
        {
            ShowError("The selected cover image could not be loaded.");
            return;
        }

        _replacementCoverPath = path;
        _cover.Texture = texture;
        UpdatePendingChanges();
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
