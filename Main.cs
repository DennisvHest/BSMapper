using BSMapper;
using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Control
{
    private const string ResumeMapArgument = "--bsmapper-resume-map=";
    private const string ResumeDifficultyArgument = "--bsmapper-resume-difficulty=";

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
        _mapDetails.CoverChanged += RefreshMapList;
        GetNode<ConfirmationDialog>("HeadsetRequiredDialog").Confirmed += RestartForVr;

        LoadSettings();
        if (IsValidInstallLocation(BeatSaberInstallLocation))
        {
            ConfigureInstallLocation(BeatSaberInstallLocation);
        }
        else
        {
            ShowInstallLocationScreen();
        }

        // Scene changes must wait until the main scene has finished entering the tree.
        Callable.From(ResumeEditorAfterRestart).CallDeferred();
    }

    private void Start()
    {
        if (!DebugWithoutVr)
        {
            var xrInterface = XRServer.FindInterface("OpenXR");
            if (xrInterface is null || !xrInterface.IsInitialized())
            {
                GD.Print("OpenXR not initialized, please check if your headset is connected");
                GetNode<AcceptDialog>("HeadsetRequiredDialog").PopupCentered();
                return;
            }

            GD.Print("OpenXR initialized successfully");
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            GetViewport().UseXR = true;
        }

        GetTree().ChangeSceneToFile("res://editor/editor.tscn");
    }

    private void RestartForVr()
    {
        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        if (manager.CurrentBeatmapInfo is null || manager.CurrentBeatmapDifficultyInfo is null)
        {
            GetNode<AcceptDialog>("EditorResumeErrorDialog").PopupCentered();
            return;
        }

        var arguments = new List<string>();
        foreach (var argument in OS.GetCmdlineArgs())
        {
            if (argument is "--" or "++")
                break;
            arguments.Add(argument);
        }

        // An unexported project must restart in the project, not the project manager.
        if (OS.HasFeature("editor") && !arguments.Contains("--path"))
        {
            arguments.Add("--path");
            arguments.Add(ProjectSettings.GlobalizePath("res://"));
        }

        arguments.Add("--");
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (!argument.StartsWith(ResumeMapArgument, StringComparison.Ordinal)
                && !argument.StartsWith(ResumeDifficultyArgument, StringComparison.Ordinal))
                arguments.Add(argument);
        }
        arguments.Add(ResumeMapArgument + manager.CurrentBeatmapInfo.FilePath);
        arguments.Add(ResumeDifficultyArgument + manager.CurrentBeatmapDifficultyInfo.BeatMapFileName);

        OS.SetRestartOnExit(true, arguments.ToArray());
        GetTree().Quit();
    }

    private void ResumeEditorAfterRestart()
    {
        string infoPath = null;
        string difficultyFileName = null;
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(ResumeMapArgument, StringComparison.Ordinal))
                infoPath = argument[ResumeMapArgument.Length..];
            else if (argument.StartsWith(ResumeDifficultyArgument, StringComparison.Ordinal))
                difficultyFileName = argument[ResumeDifficultyArgument.Length..];
        }

        // Only an explicitly requested restart resumes a map; normal launches do not.
        if (infoPath is null && difficultyFileName is null)
            return;

        try
        {
            if (string.IsNullOrEmpty(infoPath) || string.IsNullOrEmpty(difficultyFileName)
                || !FileAccess.FileExists(infoPath))
                throw new InvalidOperationException("The restart map is missing or the restart arguments are incomplete.");

            var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
            var info = manager.LoadBeatmapInfo(infoPath);
            foreach (var set in info.DifficultyBeatMapSets)
            {
                foreach (var difficulty in set.DifficultyBeatMaps)
                {
                    if (difficulty.BeatMapFileName != difficultyFileName)
                        continue;
                    if (!FileAccess.FileExists(info.MapFolder.PathJoin(difficultyFileName)))
                        throw new InvalidOperationException("The selected difficulty file no longer exists.");

                    manager.LoadDifficulty(difficulty);
                    Start();
                    return;
                }
            }
            throw new InvalidOperationException("The selected difficulty is no longer in the map.");
        }
        catch (Exception exception)
        {
            GD.PushError($"Unable to resume editor after restart: {exception.Message}");
            GetNode<AcceptDialog>("EditorResumeErrorDialog").PopupCentered();
        }
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