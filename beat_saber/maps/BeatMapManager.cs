using System;
using Godot;

public partial class BeatMapManager : Node
{
    [Signal]
    public delegate void CurrentBeatmapInfoChangedEventHandler(BeatMapInfo beatmapInfo);

    [Signal]
    public delegate void CurrentBeatmapDifficultyInfoChangedEventHandler(BeatMapDifficultyInfo difficulty);

    [Signal]
    public delegate void CurrentBeatmapChangedEventHandler(BeatMap beatmap);

    [Export]
    public BeatMapInfo CurrentBeatmapInfo { get; set; }

    [Export]
    public BeatMapDifficultyInfo CurrentBeatmapDifficultyInfo { get; set; }

    [Export]
    public string CurrentBeatmapFilePath { get; set; } = string.Empty;

    [Export]
    public BeatMap CurrentBeatmap { get; set; }

    [Export]
    public string WipBeatmapLocation { get; set; } = string.Empty;

    public void SetWipBeatmapLocation(string filePath)
    {
        WipBeatmapLocation = filePath;
    }

    public BeatMapInfo NewMap(
        string songPath,
        string songName,
        string songSubName,
        string songAuthorName,
        float bpm)
    {
        var mapFolder = WipBeatmapLocation.PathJoin(GetMapFolderName(songName, songAuthorName));
        var directory = DirAccess.Open(WipBeatmapLocation)
            ?? throw new InvalidOperationException($"Failed to open beatmap directory: {WipBeatmapLocation}");

        if (DirAccess.DirExistsAbsolute(mapFolder))
        {
            ClearDirectory(mapFolder);
        }

        var makeDirectoryError = directory.MakeDirRecursive(mapFolder);
        if (makeDirectoryError != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to create beatmap directory: {mapFolder}");
        }

        const string songFileName = "song.egg";
        var songDestinationPath = mapFolder.PathJoin(songFileName);
        using var sourceFile = FileAccess.Open(songPath, FileAccess.ModeFlags.Read)
            ?? throw new InvalidOperationException($"Failed to open source song file: {songPath}");
        var songData = sourceFile.GetBuffer(checked((long)sourceFile.GetLength()));
        using var destinationFile = FileAccess.Open(songDestinationPath, FileAccess.ModeFlags.Write)
            ?? throw new InvalidOperationException($"Failed to open destination song file: {songDestinationPath}");
        destinationFile.StoreBuffer(songData);

        var beatmapInfo = BeatMapInfo.NewMap(mapFolder, songName, songSubName, songAuthorName, songFileName, bpm);
        SaveBeatmapInfo(beatmapInfo);
        return beatmapInfo;
    }

    private static string GetMapFolderName(string songName, string songAuthorName)
    {
        var name = string.IsNullOrWhiteSpace(songAuthorName)
            ? songName
            : $"{songName} - {songAuthorName}";

        var builder = new System.Text.StringBuilder();
        foreach (var character in name.Trim())
        {
            builder.Append(char.IsLetterOrDigit(character) || character is ' ' or '-' or '_' ? character : '_');
        }

        var folderName = builder.ToString().Trim();
        return string.IsNullOrEmpty(folderName) ? "New map" : folderName;
    }

    public BeatMapDifficultyInfo NewDifficulty(
        BeatMapInfo beatmapInfo,
        BeatMapDifficultySet.BeatmapMode mode,
        BeatMapDifficultyInfo.Difficulty difficulty,
        float njs,
        float noteJumpStartBeatOffset)
    {
        var difficultyInfo = BeatMapDifficultyInfo.NewDifficulty(
            difficulty,
            mode,
            njs,
            noteJumpStartBeatOffset,
            beatmapInfo.Bpm);
        var difficultyFilePath = beatmapInfo.FilePath.PathJoin(difficultyInfo.BeatMapFileName);
        var beatmap = new BeatMap();
        beatmap.InitializeEmpty();

        using var difficultyFile = FileAccess.Open(difficultyFilePath, FileAccess.ModeFlags.Write)
            ?? throw new InvalidOperationException($"Failed to open difficulty file for writing: {difficultyFilePath}");
        difficultyFile.StoreString(Json.Stringify(beatmap.OriginalMap, string.Empty, false));

        beatmapInfo.AddDifficulty(difficultyInfo, mode);
        SaveBeatmapInfo(beatmapInfo);
        return difficultyInfo;
    }

    public BeatMapInfo LoadBeatmapInfo(string filePath)
    {
        CurrentBeatmapInfo = ReadBeatmapInfo(filePath);
        EmitSignal(SignalName.CurrentBeatmapInfoChanged, CurrentBeatmapInfo);
        return CurrentBeatmapInfo;
    }

    public BeatMapInfo ReadBeatmapInfo(string filePath)
    {
        var original = ParseJsonFile(filePath);
        return BeatMapInfo.FromFile(original, filePath);
    }

    public void LoadDifficulty(BeatMapDifficultyInfo difficulty)
    {
        CurrentBeatmapDifficultyInfo = difficulty;
        EmitSignal(SignalName.CurrentBeatmapDifficultyInfoChanged, difficulty);
        var difficultyFilePath = CurrentBeatmapInfo.FilePath.GetBaseDir().PathJoin(difficulty.BeatMapFileName);
        LoadBeatmap(difficultyFilePath);
    }

    public void ChangeBeatmap(BeatMap beatmap)
    {
        CurrentBeatmap = beatmap;
        EmitSignal(SignalName.CurrentBeatmapChanged, beatmap);
    }

    public void SaveBeatmap()
    {
        CurrentBeatmap.SaveChanges();
        using var beatmapFile = FileAccess.Open(CurrentBeatmapFilePath, FileAccess.ModeFlags.Write)
            ?? throw new InvalidOperationException($"Failed to open beatmap file for writing: {CurrentBeatmapFilePath}");
        beatmapFile.StoreString(Json.Stringify(CurrentBeatmap.OriginalMap, string.Empty, false));
    }

    private static void ClearDirectory(string directoryPath)
    {
        var directory = DirAccess.Open(directoryPath)
            ?? throw new InvalidOperationException($"Failed to open directory for clearing: {directoryPath}");
        directory.ListDirBegin();

        for (var itemName = directory.GetNext(); !string.IsNullOrEmpty(itemName); itemName = directory.GetNext())
        {
            if (itemName is "." or "..")
            {
                continue;
            }

            var itemPath = directoryPath.PathJoin(itemName);
            if (directory.CurrentIsDir())
            {
                ClearDirectory(itemPath);
            }

            var removeError = DirAccess.RemoveAbsolute(itemPath);
            if (removeError != Error.Ok)
            {
                throw new InvalidOperationException($"Failed to remove: {itemPath}");
            }
        }

        directory.ListDirEnd();
    }

    private static void SaveBeatmapInfo(BeatMapInfo beatmapInfo)
    {
        var infoPath = beatmapInfo.FilePath.PathJoin("info.dat");
        using var infoFile = FileAccess.Open(infoPath, FileAccess.ModeFlags.Write)
            ?? throw new InvalidOperationException($"Failed to open info.dat for writing: {infoPath}");
        infoFile.StoreString(Json.Stringify(beatmapInfo.OriginalObject, string.Empty, false));
    }

    private void LoadBeatmap(string filePath)
    {
        var original = ParseJsonFile(filePath);
        CurrentBeatmapFilePath = filePath;
        var beatmap = new BeatMap();
        beatmap.LoadFromFile(original);
        ChangeBeatmap(beatmap);
    }

    private static Variant ParseJsonFile(string filePath)
    {
        var content = FileAccess.GetFileAsString(filePath);
        var json = new Json();
        var result = json.Parse(content);
        if (result != Error.Ok)
        {
            throw new InvalidOperationException(
                $"JSON Parse Error: {json.GetErrorMessage()} in {filePath} at line {json.GetErrorLine()}");
        }

        return json.Data;
    }
}