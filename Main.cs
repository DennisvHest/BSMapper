using Godot;
using System.Collections.Generic;

public partial class Main : Control
{
    private const string SettingsFilePath = "user://settings.cfg";
    private const string CustomLevelsFolder = "Beat Saber_Data/CustomLevels";
    private const string CustomWipLevelsFolder = "Beat Saber_Data/CustomWIPLevels";
    private const int ButtonsPerFrame = 50;
    private const int HydrationsPerFrame = 4;

    [Export]
    public PackedScene StartScene { get; set; }

    [Export]
    public bool DebugWithoutVr { get; set; }

    public string BeatSaberInstallLocation { get; set; } = string.Empty;

    private bool _showWipMaps = true;
    private string _searchText = string.Empty;
    private readonly Queue<(string FolderName, string MapFolder, string InfoPath)> _pendingButtons = new();
    private readonly List<MapListEntry> _pendingHydrations = new();

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

        GetTree().ChangeSceneToPacked(StartScene);
    }

    private void LoadSettings()
    {
        var config = new ConfigFile();
        if (config.Load(SettingsFilePath) == Error.Ok)
        {
            BeatSaberInstallLocation = config.GetValue("settings", "install_location", string.Empty).AsString();
        }
    }

    private void SaveSettings()
    {
        var config = new ConfigFile();
        config.SetValue("settings", "install_location", BeatSaberInstallLocation);
        config.Save(SettingsFilePath);
    }

    private static bool IsValidInstallLocation(string installLocation)
    {
        return !string.IsNullOrEmpty(installLocation)
            && DirAccess.DirExistsAbsolute(installLocation.PathJoin(CustomWipLevelsFolder));
    }

    private void ConfigureInstallLocation(string directory)
    {
        BeatSaberInstallLocation = directory;
        SaveSettings();
        GetNode<BeatMapManager>("/root/BeatMapManager")
            .SetWipBeatmapLocation(directory.PathJoin(CustomWipLevelsFolder));
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
        GetNode<Label>("HomeScreen/VBox/Header/InstallLocationLabel").Text = BeatSaberInstallLocation;
        RefreshMapList();
    }

    private void RefreshMapList()
    {
        _pendingButtons.Clear();
        _pendingHydrations.Clear();

        var mapList = GetNode<VBoxContainer>("HomeScreen/VBox/MapScroll/MapList");
        foreach (var child in mapList.GetChildren())
        {
            child.QueueFree();
        }

        var mapsLocation = BeatSaberInstallLocation
            .PathJoin(_showWipMaps ? CustomWipLevelsFolder : CustomLevelsFolder);

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

                    _pendingButtons.Enqueue((folderName, mapFolder, infoPath));
                }
            }
        }

        GetNode<Label>("HomeScreen/VBox/EmptyLabel").Visible = _pendingButtons.Count == 0;
    }

    public override void _Process(double delta)
    {
        ProcessPendingButtons();
        ProcessPendingHydrations();
    }

    private void ProcessPendingButtons()
    {
        if (_pendingButtons.Count == 0)
        {
            return;
        }

        var mapList = GetNode<VBoxContainer>("HomeScreen/VBox/MapScroll/MapList");
        for (var i = 0; i < ButtonsPerFrame && _pendingButtons.Count > 0; i++)
        {
            var (folderName, mapFolder, infoPath) = _pendingButtons.Dequeue();
            mapList.AddChild(CreateMapButton(folderName, mapFolder, infoPath));
        }
    }

    private void ProcessPendingHydrations()
    {
        if (_pendingHydrations.Count == 0)
        {
            return;
        }

        var scroll = GetNode<ScrollContainer>("HomeScreen/VBox/MapScroll");
        var visibleRect = scroll.GetGlobalRect();
        var hydrated = 0;

        for (var i = _pendingHydrations.Count - 1; i >= 0 && hydrated < HydrationsPerFrame; i--)
        {
            var entry = _pendingHydrations[i];
            if (!IsInstanceValid(entry.Button))
            {
                _pendingHydrations.RemoveAt(i);
                continue;
            }

            if (!entry.Button.Visible || !visibleRect.Intersects(entry.Button.GetGlobalRect()))
            {
                continue;
            }

            HydrateEntry(entry);
            _pendingHydrations.RemoveAt(i);
            hydrated++;
        }
    }

    private static void HydrateEntry(MapListEntry entry)
    {
        entry.Cover.Texture = LoadCoverImage(entry.MapFolder, entry.CoverImageFileName);
        entry.DurationLabel.Text = GetSongDurationText(entry.MapFolder, entry.SongFileName);
    }

    private Button CreateMapButton(string mapName, string mapFolder, string infoPath)
    {
        var songName = mapName;
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

        var button = new Button
        {
            TooltipText = songName,
            CustomMinimumSize = new Vector2(0, 80),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        button.SetMeta("search_text", $"{songName}\n{songAuthor}".ToLowerInvariant());
        button.Visible = MatchesSearch(button);

        var normalStyle = new StyleBoxFlat { BgColor = new Color(0.11f, 0.125f, 0.176f) };
        normalStyle.SetCornerRadiusAll(8);
        var hoverStyle = (StyleBoxFlat)normalStyle.Duplicate();
        hoverStyle.BgColor = new Color(0.153f, 0.176f, 0.243f);
        var pressedStyle = (StyleBoxFlat)normalStyle.Duplicate();
        pressedStyle.BgColor = new Color(0.153f, 0.459f, 0.937f);
        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        button.AddThemeStyleboxOverride("pressed", pressedStyle);

        var content = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        content.SetAnchorsPreset(LayoutPreset.FullRect);
        content.AddThemeConstantOverride("separation", 16);
        content.OffsetLeft = 12;
        content.OffsetRight = -12;
        content.OffsetTop = 8;
        content.OffsetBottom = -8;
        button.AddChild(content);

        var cover = new TextureRect
        {
            CustomMinimumSize = new Vector2(64, 64),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        content.AddChild(cover);

        var textColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        textColumn.AddThemeConstantOverride("separation", 2);
        content.AddChild(textColumn);

        var titleLabel = new Label
        {
            Text = titleText,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.96f, 0.98f));
        textColumn.AddChild(titleLabel);

        var authorLabel = new Label
        {
            Text = string.IsNullOrEmpty(songAuthor) ? "Unknown artist" : songAuthor,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        authorLabel.AddThemeFontSizeOverride("font_size", 14);
        authorLabel.AddThemeColorOverride("font_color", new Color(0.62f, 0.66f, 0.74f));
        textColumn.AddChild(authorLabel);

        var durationLabel = new Label
        {
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        durationLabel.AddThemeFontSizeOverride("font_size", 14);
        durationLabel.AddThemeColorOverride("font_color", new Color(0.62f, 0.66f, 0.74f));
        content.AddChild(durationLabel);

        button.Pressed += () => OpenMapInEditor(infoPath);

        _pendingHydrations.Add(new MapListEntry
        {
            Button = button,
            Cover = cover,
            DurationLabel = durationLabel,
            MapFolder = mapFolder,
            CoverImageFileName = coverImageFileName,
            SongFileName = songFileName,
        });

        return button;
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

    private static string GetSongDurationText(string mapFolder, string songFileName)
    {
        if (string.IsNullOrEmpty(songFileName))
        {
            return string.Empty;
        }

        var songPath = mapFolder.PathJoin(songFileName);
        if (!FileAccess.FileExists(songPath))
        {
            return string.Empty;
        }

        var stream = AudioStreamOggVorbis.LoadFromFile(songPath);
        if (stream is null)
        {
            return string.Empty;
        }

        var totalSeconds = (int)Mathf.Round(stream.GetLength());
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }

    private void OpenMapInEditor(string infoPath)
    {
        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        var beatmapInfo = manager.LoadBeatmapInfo(infoPath);
        if (beatmapInfo.DifficultyBeatMapSets.Count == 0
            || beatmapInfo.DifficultyBeatMapSets[0].DifficultyBeatMaps.Count == 0)
        {
            GD.PushWarning($"Map has no difficulties to edit: {infoPath}");
            return;
        }

        manager.LoadDifficulty(beatmapInfo.DifficultyBeatMapSets[0].DifficultyBeatMaps[0]);
        Start();
    }

    private void OnSelectInstallLocationPressed()
    {
        GetNode<FileDialog>("InstallLocationFolderDialog").Show();
    }

    private void OnNewMapButtonPressed()
    {
        GetNode<FileDialog>("SongFileDialog").Show();
    }

    private void OnSongFileDialogFileSelected(string path)
    {
        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        if (string.IsNullOrEmpty(manager.WipBeatmapLocation))
        {
            GD.PushWarning("Cannot create a map before a Beat Saber install location is configured.");
            return;
        }

        var newBeatMap = manager.NewMap(path);
        manager.NewDifficulty(
            newBeatMap,
            BeatMapDifficultySet.BeatmapMode.Standard,
            BeatMapDifficultyInfo.Difficulty.Expert,
            16.0f,
            -0.15f);
        OpenMapInEditor(newBeatMap.FilePath.PathJoin("info.dat"));
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
                $"The selected folder does not contain \"{CustomWipLevelsFolder}\". Please select the Beat Saber install folder.";
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