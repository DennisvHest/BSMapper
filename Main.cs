using BSMapper;
using Godot;
using System.Collections.Generic;

public partial class Main : Control
{
    [Export]
    public bool DebugWithoutVr { get; set; }

    public string BeatSaberInstallLocation { get; set; } = string.Empty;

    private MapList _mapList;
    private MapDetails _mapDetails;

    public override void _Ready()
    {
        _mapList = GetNode<MapList>("%MapList");
        _mapDetails = GetNode<MapDetails>("%MapDetails");

        _mapList.MapSelected += MapSelected;
        _mapList.NewMapRequested += OnNewMapRequested;
        _mapDetails.OpenMapRequested += OpenMapInEditor;
        _mapDetails.MapCreated += OnMapCreated;
        _mapDetails.MapDeleted += OnMapDeleted;

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
        var mapsLocation = BeatSaberInstallLocation
            .PathJoin(Settings.CustomWipLevelsFolder);
        _mapDetails.Hide();
        _mapList.Refresh(mapsLocation);
    }

    private void MapSelected(string infoPath)
    {
        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        _mapDetails.Populate(manager, manager.ReadBeatmapInfo(infoPath));
    }

    private void OnNewMapRequested()
    {
        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        _mapDetails.BeginCreate(manager);
    }

    private void OnMapCreated()
    {
        var mapsLocation = BeatSaberInstallLocation.PathJoin(Settings.CustomWipLevelsFolder);
        _mapList.Refresh(mapsLocation);
    }

    private void OnMapDeleted()
    {
        var mapsLocation = BeatSaberInstallLocation.PathJoin(Settings.CustomWipLevelsFolder);
        _mapList.Refresh(mapsLocation);
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

}