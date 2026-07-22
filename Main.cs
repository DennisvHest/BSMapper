using Godot;

public partial class Main : Control
{
    private const string SettingsFilePath = "user://settings.cfg";
    private const string CustomLevelsFolder = "Beat Saber_Data/CustomLevels";
    private const string CustomWipLevelsFolder = "Beat Saber_Data/CustomWIPLevels";

    [Export]
    public PackedScene StartScene { get; set; }

    [Export]
    public bool DebugWithoutVr { get; set; }

    public string BeatSaberInstallLocation { get; set; } = string.Empty;

    private bool _showWipMaps = true;

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
        var mapList = GetNode<VBoxContainer>("HomeScreen/VBox/MapScroll/MapList");
        foreach (var child in mapList.GetChildren())
        {
            child.QueueFree();
        }

        var mapsLocation = BeatSaberInstallLocation
            .PathJoin(_showWipMaps ? CustomWipLevelsFolder : CustomLevelsFolder);
        var mapCount = 0;

        if (DirAccess.DirExistsAbsolute(mapsLocation))
        {
            using var directory = DirAccess.Open(mapsLocation);
            if (directory is not null)
            {
                foreach (var folderName in directory.GetDirectories())
                {
                    var infoPath = mapsLocation.PathJoin(folderName).PathJoin("info.dat");
                    if (!FileAccess.FileExists(infoPath))
                    {
                        infoPath = mapsLocation.PathJoin(folderName).PathJoin("Info.dat");
                        if (!FileAccess.FileExists(infoPath))
                        {
                            continue;
                        }
                    }

                    mapList.AddChild(CreateMapButton(folderName, infoPath));
                    mapCount++;
                }
            }
        }

        GetNode<Label>("HomeScreen/VBox/EmptyLabel").Visible = mapCount == 0;
    }

    private Button CreateMapButton(string mapName, string infoPath)
    {
        var button = new Button
        {
            Text = mapName,
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new Vector2(0, 56),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        button.AddThemeFontSizeOverride("font_size", 18);

        var normalStyle = new StyleBoxFlat { BgColor = new Color(0.11f, 0.125f, 0.176f) };
        normalStyle.SetCornerRadiusAll(8);
        normalStyle.ContentMarginLeft = 20;
        var hoverStyle = (StyleBoxFlat)normalStyle.Duplicate();
        hoverStyle.BgColor = new Color(0.153f, 0.176f, 0.243f);
        var pressedStyle = (StyleBoxFlat)normalStyle.Duplicate();
        pressedStyle.BgColor = new Color(0.153f, 0.459f, 0.937f);
        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        button.AddThemeStyleboxOverride("pressed", pressedStyle);

        button.Pressed += () => OpenMapInEditor(infoPath);
        return button;
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
}