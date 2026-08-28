using BSMapper;
using Godot;
using System.Collections.Generic;

public partial class Main : Control
{
    private const int ButtonsPerFrame = 50;
    private const int HydrationsPerFrame = 4;
    private const string NewMapDialogPath = "NewMapDialog";

    [Export]
    public bool DebugWithoutVr { get; set; }

    public string BeatSaberInstallLocation { get; set; } = string.Empty;

    private bool _showWipMaps = true;
    private string _searchText = string.Empty;
    private string _newMapSongPath = string.Empty;
    private BeatMapInfo _editingBeatmapInfo;
    private readonly Queue<(string FolderName, string MapFolder, string InfoPath)> _mapListItems = new();
    private readonly List<MapListEntry> _pendingHydrations = new();

    private ItemList _mapList;
    private Container _mapDetails;

    private sealed class MapListEntry
    {
        public Button Button;
        public TextureRect Cover;
        public Label DurationLabel;
        public string MapFolder;
        public string CoverImageFileName;
        public string SongFileName;
    }

    public override void _Ready()
    {
        _mapList = GetNode<ItemList>("%MapList");
        _mapDetails = GetNode<Container>("%MapDetails");

        _mapList.ItemSelected += MapSelected;

        LoadSettings();
        if (IsValidInstallLocation(BeatSaberInstallLocation))
        {
            ConfigureInstallLocation(BeatSaberInstallLocation);
        }
        else
        {
            ShowInstallLocationScreen();
        }
    }

    private void Start()
    {
        var xrInterface = XRServer.FindInterface("OpenXR");
        if (!DebugWithoutVr && xrInterface is not null && xrInterface.IsInitialized())
        {
            GD.Print("OpenXR initialized successfully");
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            GetViewport().UseXR = true;
        }
        else
        {
            GD.Print("OpenXR not initialized, please check if your headset is connected");
        }

        GetTree().ChangeSceneToFile("res://editor/editor.tscn");
    }

    private void LoadSettings()
    {
        var settings = Settings.LoadSettings();
        BeatSaberInstallLocation = settings.GetValue(SettingSections.Settings, SettingsKeys.BeatSaberInstallLocation, string.Empty).AsString();
    }

    private void SaveSettings()
    {
        var settings = Settings.GetSettings();
        settings.SetValue(SettingSections.Settings, SettingsKeys.BeatSaberInstallLocation, BeatSaberInstallLocation);
        Settings.SaveSettings();
    }

    private static bool IsValidInstallLocation(string installLocation)
    {
        return !string.IsNullOrEmpty(installLocation)
            && DirAccess.DirExistsAbsolute(installLocation.PathJoin(Settings.CustomWipLevelsFolder));
    }

    private void ConfigureInstallLocation(string directory)
    {
        BeatSaberInstallLocation = directory;
        SaveSettings();
        GetNode<FileDialog>("InstallLocationFolderDialog").CurrentDir = directory;
        ShowHomeScreen();
    }

    private void ShowInstallLocationScreen()
    {
        GetNode<Control>("HomeScreen").Hide();
        GetNode<Control>("InstallLocationScreen").Show();
    }

    private void ShowHomeScreen()
    {
        GetNode<Control>("InstallLocationScreen").Hide();
        GetNode<Control>("HomeScreen").Show();
        RefreshMapList();
    }

    private void RefreshMapList()
    {
        _mapListItems.Clear();
        _pendingHydrations.Clear();

        _mapList.Clear();
        foreach (var child in _mapList.GetChildren())
        {
            child.QueueFree();
        }

        var mapsLocation = BeatSaberInstallLocation
            .PathJoin(_showWipMaps ? Settings.CustomWipLevelsFolder : Settings.CustomLevelsFolder);

        if (DirAccess.DirExistsAbsolute(mapsLocation))
        {
            using var directory = DirAccess.Open(mapsLocation);
            if (directory is not null)
            {
                foreach (var folderName in directory.GetDirectories())
                {
                    var mapFolder = mapsLocation.PathJoin(folderName);
                    var infoPath = mapFolder.PathJoin("info.dat");
                    if (!FileAccess.FileExists(infoPath))
                    {
                        infoPath = mapFolder.PathJoin("Info.dat");
                        if (!FileAccess.FileExists(infoPath))
                        {
                            continue;
                        }
                    }

                    _mapListItems.Enqueue((folderName, mapFolder, infoPath));
                }
            }
        }

        // GetNode<Label>("HomeScreen/VBox/EmptyLabel").Visible = _pendingButtons.Count == 0; TODO
    }

    public override void _Process(double delta)
    {
        ProcessPendingMapListItems();
    }

    private void ProcessPendingMapListItems()
    {
        if (_mapListItems.Count == 0)
        {
            return;
        }

        for (var i = 0; i < ButtonsPerFrame && _mapListItems.Count > 0; i++)
        {
            var (folderName, mapFolder, infoPath) = _mapListItems.Dequeue();

            var songName = folderName;
            var songAuthor = string.Empty;
            var coverImageFileName = string.Empty;
            var songFileName = string.Empty;

            var json = new Json();
            if (json.Parse(FileAccess.GetFileAsString(infoPath)) == Error.Ok)
            {
                var data = json.Data.AsGodotDictionary();
                if (data.TryGetValue("_songName", out var songNameValue) && !string.IsNullOrEmpty(songNameValue.AsString()))
                {
                    songName = songNameValue.AsString();
                }

                if (data.TryGetValue("_songAuthorName", out var authorValue))
                {
                    songAuthor = authorValue.AsString();
                }

                if (data.TryGetValue("_coverImageFilename", out var coverValue))
                {
                    coverImageFileName = coverValue.AsString();
                }

                if (data.TryGetValue("_songFilename", out var songFileValue))
                {
                    songFileName = songFileValue.AsString();
                }
            }

            const int maxTitleLength = 100;
            var titleText = songName.Length > maxTitleLength
                ? songName[..maxTitleLength] + "..."
                : songName;

            Texture2D coverTexture = null;

            var coverImagePath = !string.IsNullOrWhiteSpace(coverImageFileName)
                ? mapFolder.PathJoin(coverImageFileName)
                : "res://icon.svg";

            var coverImage = new Image();

            var result = coverImage.Load(coverImagePath);

            if (result == Error.Ok)
            {
                coverTexture = ImageTexture.CreateFromImage(coverImage);
            }
            else
            {
                if (coverImage.Load("res://icon.svg") == Error.Ok)
                {
                    coverTexture = ImageTexture.CreateFromImage(coverImage);
                }
            }


            _mapList.AddItem(titleText, coverTexture);
        }
    }

    private void MapSelected(long index)
    {
        var songName = _mapList.GetItemText((int)index);
        var coverTexture = _mapList.GetItemIcon((int)index);

        _mapDetails.Show();

        var songNameEdit = (LineEdit)_mapDetails.FindChild("SongNameEdit", true, false);
        songNameEdit.Text = songName;

        var coverImage = (TextureRect)_mapDetails.FindChild("Cover", true, false);
        coverImage.Texture = coverTexture;
    }

    private static Texture2D LoadCoverImage(string mapFolder, string coverImageFileName)
    {
        if (string.IsNullOrEmpty(coverImageFileName))
        {
            return null;
        }

        var coverPath = mapFolder.PathJoin(coverImageFileName);
        if (!FileAccess.FileExists(coverPath))
        {
            return null;
        }

        var image = Image.LoadFromFile(coverPath);
        if (image is null)
        {
            return null;
        }

        return ImageTexture.CreateFromImage(image);
    }

    private void OpenMapInEditor(string infoPath)
    {
        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        var beatmapInfo = manager.LoadBeatmapInfo(infoPath);

        var difficulties = new List<BeatMapDifficultyInfo>();
        foreach (var set in beatmapInfo.DifficultyBeatMapSets)
        {
            foreach (var difficulty in set.DifficultyBeatMaps)
            {
                difficulties.Add(difficulty);
            }
        }

        if (difficulties.Count == 0)
        {
            GD.PushWarning($"Map has no difficulties to edit: {infoPath}");
            return;
        }

        if (difficulties.Count == 1)
        {
            manager.LoadDifficulty(difficulties[0]);
            Start();
            return;
        }

        ShowDifficultySelectDialog(manager, difficulties);
    }

    private void ShowDifficultySelectDialog(BeatMapManager manager, List<BeatMapDifficultyInfo> difficulties)
    {
        var dialog = GetNode<AcceptDialog>("DifficultySelectDialog");
        var list = GetNode<VBoxContainer>("DifficultySelectDialog/Margin/DifficultyList");
        foreach (var child in list.GetChildren())
        {
            list.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var difficulty in difficulties)
        {
            var selected = difficulty;
            var button = new Button
            {
                Text = BeatMapDifficultyInfo.GetDifficultyName(selected.DifficultyLevel),
            };
            button.Pressed += () =>
            {
                dialog.Hide();
                manager.LoadDifficulty(selected);
                Start();
            };
            list.AddChild(button);
        }

        dialog.PopupCentered();
    }

    private void OnSelectInstallLocationPressed()
    {
        GetNode<FileDialog>("InstallLocationFolderDialog").Show();
    }

    private void OnInstallLocationFolderDialogDirSelected(string directory)
    {
        if (IsValidInstallLocation(directory))
        {
            ConfigureInstallLocation(directory);
        }
        else
        {
            var errorLabel = GetNode<Label>("InstallLocationScreen/Panel/VBox/ErrorLabel");
            errorLabel.Text =
                $"The selected folder does not contain \"{Settings.CustomWipLevelsFolder}\". Please select the Beat Saber install folder.";
            errorLabel.Show();
            ShowInstallLocationScreen();
        }
    }

    private void OnWipMapsButtonToggled(bool toggledOn)
    {
        SetMapFilter(showWipMaps: toggledOn);
    }

    private void OnCustomMapsButtonToggled(bool toggledOn)
    {
        SetMapFilter(showWipMaps: !toggledOn);
    }

    private void SetMapFilter(bool showWipMaps)
    {
        _showWipMaps = showWipMaps;
        GetNode<Button>("HomeScreen/VBox/FilterBar/WipMapsButton").SetPressedNoSignal(showWipMaps);
        GetNode<Button>("HomeScreen/VBox/FilterBar/CustomMapsButton").SetPressedNoSignal(!showWipMaps);
        RefreshMapList();
    }

    private void OnSearchTextChanged(string newText)
    {
        _searchText = newText.Trim().ToLowerInvariant();
        var mapList = GetNode<VBoxContainer>("HomeScreen/VBox/MapScroll/MapList");
        foreach (var child in mapList.GetChildren())
        {
            if (child is Button button)
            {
                button.Visible = MatchesSearch(button);
            }
        }
    }

    private bool MatchesSearch(Button button)
    {
        return string.IsNullOrEmpty(_searchText)
            || button.GetMeta("search_text", string.Empty).AsString().Contains(_searchText);
    }
}