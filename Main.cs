using System;
using Godot;

public partial class Main : Control
{
    private const string SettingsPath = "user://bsmapper.cfg";
    private const string SettingsSection = "beat_saber";
    private const string InstallLocationKey = "install_location";

    [Export]
    public PackedScene StartScene { get; set; }

    [Export]
    public bool DebugWithoutVr { get; set; }

    [Export]
    public bool DebugStartInEditor { get; set; }

    [Export(PropertyHint.GlobalDir)]
    public string BeatSaberInstallLocation { get; set; } = string.Empty;

    private XRInterface _xrInterface;
    private Control _installScreen;
    private Control _homeScreen;
    private FileDialog _installLocationDialog;
    private Label _installLocationErrorLabel;
    private VBoxContainer _mapList;
    private Label _mapListStatus;
    private Button _wipMapsButton;
    private Button _customMapsButton;
    private Button _mapButtonTemplate;
    private StyleBox _activeSourceStyle;
    private StyleBox _inactiveSourceStyle;

    public override void _Ready()
    {
        BindUiNodes();

        var savedInstallLocation = LoadInstallLocation();
        if (IsValidInstallLocation(savedInstallLocation))
        {
            ConfigureInstallLocation(savedInstallLocation);
            ShowHomeScreen();
            return;
        }

        if (IsValidInstallLocation(BeatSaberInstallLocation))
        {
            ConfigureInstallLocation(BeatSaberInstallLocation);
            ShowHomeScreen();
            return;
        }

        ShowInstallLocationScreen();
    }

    private void BindUiNodes()
    {
        _installScreen = GetNode<Control>("InstallScreen");
        _homeScreen = GetNode<Control>("HomeScreen");
        _installLocationDialog = GetNode<FileDialog>("InstallLocationDialog");
        _installLocationErrorLabel = GetNode<Label>("InstallScreen/Center/Panel/Margin/Content/ErrorLabel");
        _mapList = GetNode<VBoxContainer>("HomeScreen/Margin/Content/ListPanel/Margin/Content/Scroll/MapList");
        _mapListStatus = GetNode<Label>("HomeScreen/Margin/Content/ListPanel/Margin/Content/StatusLabel");
        _wipMapsButton = GetNode<Button>("HomeScreen/Margin/Content/Sources/WipMapsButton");
        _customMapsButton = GetNode<Button>("HomeScreen/Margin/Content/Sources/CustomMapsButton");
        _mapButtonTemplate = GetNode<Button>("MapButtonTemplate");
        _activeSourceStyle = _wipMapsButton.GetThemeStylebox("normal");
        _inactiveSourceStyle = _customMapsButton.GetThemeStylebox("normal");
    }

    private void ConfigureInstallLocation(string directory)
    {
        BeatSaberInstallLocation = directory;
        GetNode<BeatMapManager>("/root/BeatMapManager").SetWipBeatmapLocation(GetWipBeatmapsLocation(directory));
        SaveInstallLocation(directory);
    }

    private static bool IsValidInstallLocation(string directory)
    {
        return !string.IsNullOrWhiteSpace(directory) &&
               DirAccess.DirExistsAbsolute(directory.PathJoin("Beat Saber_Data"));
    }

    private static string GetWipBeatmapsLocation(string installLocation)
    {
        return installLocation.PathJoin("Beat Saber_Data/CustomWIPLevels");
    }

    private static string GetCustomBeatmapsLocation(string installLocation)
    {
        return installLocation.PathJoin("Beat Saber_Data/CustomLevels");
    }

    private static string LoadInstallLocation()
    {
        var settings = new ConfigFile();
        return settings.Load(SettingsPath) == Error.Ok
            ? settings.GetValue(SettingsSection, InstallLocationKey, string.Empty).AsString()
            : string.Empty;
    }

    private static void SaveInstallLocation(string directory)
    {
        var settings = new ConfigFile();
        settings.Load(SettingsPath);
        settings.SetValue(SettingsSection, InstallLocationKey, directory);
        var result = settings.Save(SettingsPath);
        if (result != Error.Ok)
        {
            GD.PushWarning($"Unable to save Beat Saber install location: {result}");
        }
    }

    private void SelectInstallLocation()
    {
        _installLocationDialog.CurrentDir = BeatSaberInstallLocation;
        _installLocationDialog.Show();
    }

    private void OnInstallLocationSelected(string directory)
    {
        if (!IsValidInstallLocation(directory))
        {
            ShowInstallLocationError("That folder does not contain Beat Saber_Data. Select your Beat Saber installation folder.");
            return;
        }

        ConfigureInstallLocation(directory);
        ShowHomeScreen();
    }

    private void ShowInstallLocationScreen()
    {
        _homeScreen.Hide();
        _installLocationErrorLabel.Hide();
        _installScreen.Show();
    }

    private void ShowInstallLocationError(string message)
    {
        _installLocationErrorLabel.Text = message;
        _installLocationErrorLabel.Show();
    }

    private void ShowHomeScreen()
    {
        _installScreen.Hide();
        _homeScreen.Show();
        ShowMapList(true);
    }

    private void ShowWipMaps()
    {
        ShowMapList(true);
    }

    private void ShowCustomMaps()
    {
        ShowMapList(false);
    }

    private void ShowMapList(bool showWipMaps)
    {
        var mapDirectory = showWipMaps
            ? GetWipBeatmapsLocation(BeatSaberInstallLocation)
            : GetCustomBeatmapsLocation(BeatSaberInstallLocation);
        _wipMapsButton.AddThemeStyleboxOverride("normal", showWipMaps ? _activeSourceStyle : _inactiveSourceStyle);
        _customMapsButton.AddThemeStyleboxOverride("normal", showWipMaps ? _inactiveSourceStyle : _activeSourceStyle);

        foreach (var child in _mapList.GetChildren())
        {
            child.QueueFree();
        }

        if (!DirAccess.DirExistsAbsolute(mapDirectory))
        {
            _mapListStatus.Text = $"The folder does not exist yet: {mapDirectory}";
            return;
        }

        _mapListStatus.Text = showWipMaps ? "Custom WIP Levels" : "Custom Levels";
        var directory = DirAccess.Open(mapDirectory);
        directory.ListDirBegin();
        var mapCount = 0;
        for (var name = directory.GetNext(); !string.IsNullOrEmpty(name); name = directory.GetNext())
        {
            if (!directory.CurrentIsDir() || name is "." or "..")
            {
                continue;
            }

            var infoPath = mapDirectory.PathJoin(name).PathJoin("info.dat");
            if (!FileAccess.FileExists(infoPath))
            {
                continue;
            }

            try
            {
                var mapInfo = GetNode<BeatMapManager>("/root/BeatMapManager").ReadBeatmapInfo(infoPath);
                var hasDifficulty = false;
                foreach (var set in mapInfo.DifficultyBeatMapSets)
                {
                    hasDifficulty |= set.DifficultyBeatMaps.Count > 0;
                }

                if (!hasDifficulty)
                {
                    continue;
                }

                var title = string.IsNullOrWhiteSpace(mapInfo.SongSubName)
                    ? mapInfo.SongName
                    : $"{mapInfo.SongName} — {mapInfo.SongSubName}";
                var mapButton = (Button)_mapButtonTemplate.Duplicate();
                mapButton.Text = $"{title}\n{mapInfo.SongAuthorName}";
                mapButton.Show();
                mapButton.Pressed += () => OpenMap(infoPath);
                _mapList.AddChild(mapButton);
                mapCount++;
            }
            catch (Exception exception)
            {
                GD.PushWarning($"Skipping unsupported map {infoPath}: {exception.Message}");
            }
        }

        directory.ListDirEnd();
        if (mapCount == 0)
        {
            _mapListStatus.Text = "No playable maps found in this folder.";
        }
    }

    private bool OpenMap(string infoFilePath)
    {
        try
        {
            var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
            var mapInfo = manager.LoadBeatmapInfo(infoFilePath);
            foreach (var difficultySet in mapInfo.DifficultyBeatMapSets)
            {
                if (difficultySet.DifficultyBeatMaps.Count == 0)
                {
                    continue;
                }

                manager.LoadDifficulty(difficultySet.DifficultyBeatMaps[0]);
                StartEditor();
                return true;
            }

            GD.PushWarning($"Map has no playable difficulties: {infoFilePath}");
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Unable to open map {infoFilePath}: {exception.Message}");
        }

        return false;
    }

    private void StartEditor()
    {
        _xrInterface = XRServer.FindInterface("OpenXR");
        if (!DebugWithoutVr && _xrInterface is not null && _xrInterface.IsInitialized())
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            GetViewport().UseXR = true;
        }

        GetTree().ChangeSceneToPacked(StartScene);
    }
}