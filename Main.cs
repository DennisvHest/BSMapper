using Godot;

public partial class Main : Control
{
    [Export]
    public PackedScene StartScene { get; set; }

    [Export]
    public bool DebugWithoutVr { get; set; }

    [Export]
    public bool DebugStartInEditor { get; set; }

    [Export(PropertyHint.GlobalDir)]
    public string BeatSaberInstallLocation { get; set; } = string.Empty;

    [Export(PropertyHint.File, "*.ogg,*.egg")]
    public string DefaultSongFile { get; set; } = string.Empty;

    private XRInterface _xrInterface;

    public override void _Ready()
    {
        ApplyExportedConfiguration();
        if (DebugStartInEditor && LoadDefaultMap())
        {
            CallDeferred(MethodName.Start);
        }
    }

    private void Start()
    {
        _xrInterface = XRServer.FindInterface("OpenXR");
        if (!DebugWithoutVr && _xrInterface is not null && _xrInterface.IsInitialized())
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

    private void ApplyExportedConfiguration()
    {
        if (!string.IsNullOrEmpty(BeatSaberInstallLocation))
        {
            GetNode<FileDialog>("InstallLocationSelector/InstallLocationFolderDialog").CurrentDir = BeatSaberInstallLocation;
            var wipBeatmapsLocation = GetWipBeatmapsLocation(BeatSaberInstallLocation);
            if (DirAccess.DirExistsAbsolute(wipBeatmapsLocation))
            {
                ConfigureInstallLocation(BeatSaberInstallLocation);
            }
            else
            {
                GD.PushWarning(
                    $"Configured Beat Saber install location does not contain Beat Saber_Data/CustomWIPLevels: {BeatSaberInstallLocation}");
            }
        }

        if (!string.IsNullOrEmpty(DefaultSongFile))
        {
            var songDialog = GetNode<FileDialog>("MapSelector/SongFileDialog");
            songDialog.CurrentDir = DefaultSongFile.GetBaseDir();
            songDialog.CurrentFile = DefaultSongFile.GetFile();
        }
    }

    private static string GetWipBeatmapsLocation(string installLocation)
    {
        return installLocation.PathJoin("Beat Saber_Data/CustomWIPLevels");
    }

    private void ConfigureInstallLocation(string directory)
    {
        BeatSaberInstallLocation = directory;
        var wipBeatmapsLocation = GetWipBeatmapsLocation(directory);
        GetNode<BeatMapManager>("/root/BeatMapManager").SetWipBeatmapLocation(wipBeatmapsLocation);
        GetNode<Label>("InstallLocationSelector/InstallLocationLabel").Text = wipBeatmapsLocation;
        GetNode<Control>("MapSelector").Show();
    }

    private bool LoadDefaultMap()
    {
        if (string.IsNullOrEmpty(BeatSaberInstallLocation))
        {
            GD.PushWarning("Debug start is enabled, but no Beat Saber install location is configured on the main node.");
            return false;
        }

        if (!DirAccess.DirExistsAbsolute(BeatSaberInstallLocation))
        {
            GD.PushWarning($"Configured Beat Saber install location does not exist: {BeatSaberInstallLocation}");
            return false;
        }

        var wipBeatmapsLocation = GetWipBeatmapsLocation(BeatSaberInstallLocation);
        if (!DirAccess.DirExistsAbsolute(wipBeatmapsLocation))
        {
            GD.PushWarning(
                $"Configured Beat Saber install location is missing Beat Saber_Data/CustomWIPLevels: {BeatSaberInstallLocation}");
            return false;
        }

        if (string.IsNullOrEmpty(DefaultSongFile))
        {
            GD.PushWarning("Debug start is enabled, but no default song file is configured on the main node.");
            return false;
        }

        if (!FileAccess.FileExists(DefaultSongFile))
        {
            GD.PushWarning($"Configured default song file does not exist: {DefaultSongFile}");
            return false;
        }

        ConfigureInstallLocation(BeatSaberInstallLocation);
        return LoadSelectedSong(DefaultSongFile);
    }

    private bool LoadSelectedSong(string path)
    {
        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        if (string.IsNullOrEmpty(manager.WipBeatmapLocation))
        {
            GD.PushWarning("Cannot create a map before a Beat Saber install location is configured.");
            return false;
        }

        DefaultSongFile = path;
        var newBeatMap = manager.NewMap(path);
        manager.NewDifficulty(
            newBeatMap,
            BeatMapDifficultySet.BeatmapMode.Standard,
            BeatMapDifficultyInfo.Difficulty.Expert,
            16.0f,
            -0.15f);
        var beatmapInfo = manager.LoadBeatmapInfo(newBeatMap.FilePath.PathJoin("info.dat"));
        manager.LoadDifficulty(beatmapInfo.DifficultyBeatMapSets[0].DifficultyBeatMaps[0]);
        return true;
    }

    private void OnInstallLocationButtonPressed()
    {
        GetNode<FileDialog>("InstallLocationSelector/InstallLocationFolderDialog").Show();
    }

    private void OnInstallLocationFolderDialogDirSelected(string directory)
    {
        ConfigureInstallLocation(directory);
    }

    private void OnNewMapButtonPressed()
    {
        GetNode<FileDialog>("MapSelector/SongFileDialog").Show();
    }

    private void OnSongFileDialogFileSelected(string path)
    {
        if (LoadSelectedSong(path))
        {
            Start();
        }
    }
}