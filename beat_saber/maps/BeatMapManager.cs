using System;
using System.Collections.Generic;
using BSMapper;
using Godot;

public partial class BeatMapManager : Node
{
    /// <summary>Note jump settings for a single difficulty, as entered by the user.</summary>
    public readonly record struct DifficultySettings(
        BeatMapDifficultyInfo.Difficulty Difficulty,
        float Njs,
        float NoteJumpStartBeatOffset);

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


    public BeatMapInfo NewMap(
        string songPath,
        string songName,
        string songSubName,
        string songAuthorName,
        float bpm)
    {
        var mapFolder = Settings.WipBeatmapLocation.PathJoin(GetMapFolderName(songName, songAuthorName));
        var directory = DirAccess.Open(Settings.WipBeatmapLocation)
            ?? throw new InvalidOperationException($"Failed to open beatmap directory: {Settings.WipBeatmapLocation}");

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
        CopySongFile(songPath, mapFolder.PathJoin(songFileName));

        var beatmapInfo = BeatMapInfo.NewMap(mapFolder, songName, songSubName, songAuthorName, songFileName, bpm);
        SaveBeatmapInfo(beatmapInfo);
        return beatmapInfo;
    }

    private static void CopySongFile(string songPath, string songDestinationPath)
    {
        using var sourceFile = FileAccess.Open(songPath, FileAccess.ModeFlags.Read)
            ?? throw new InvalidOperationException($"Failed to open source song file: {songPath}");
        var songData = sourceFile.GetBuffer(checked((long)sourceFile.GetLength()));
        using var destinationFile = FileAccess.Open(songDestinationPath, FileAccess.ModeFlags.Write)
            ?? throw new InvalidOperationException($"Failed to open destination song file: {songDestinationPath}");
        destinationFile.StoreBuffer(songData);
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
        var difficultyFilePath = beatmapInfo.MapFolder.PathJoin(difficultyInfo.BeatMapFileName);
        var beatmap = new BeatMap();
        beatmap.InitializeEmpty();

        using var difficultyFile = FileAccess.Open(difficultyFilePath, FileAccess.ModeFlags.Write)
            ?? throw new InvalidOperationException($"Failed to open difficulty file for writing: {difficultyFilePath}");
        difficultyFile.StoreString(Json.Stringify(beatmap.OriginalMap, string.Empty, false));

        beatmapInfo.AddDifficulty(difficultyInfo, mode);
        SaveBeatmapInfo(beatmapInfo);
        return difficultyInfo;
    }

    /// <summary>
    /// Updates the song metadata of an existing map and replaces its audio file when a new one is
    /// given. Difficulties that are no longer selected are removed, new ones are created and
    /// existing ones get their note jump values updated.
    /// </summary>
    public void UpdateMap(
        BeatMapInfo beatmapInfo,
        string songName,
        string songSubName,
        string songAuthorName,
        float bpm,
        string songPath,
        IReadOnlyList<DifficultySettings> difficulties)
    {
        beatmapInfo.UpdateSongInfo(songName, songSubName, songAuthorName, bpm);

        if (!string.IsNullOrEmpty(songPath))
        {
            CopySongFile(songPath, beatmapInfo.MapFolder.PathJoin(beatmapInfo.SongFileName));
        }

        const BeatMapDifficultySet.BeatmapMode mode = BeatMapDifficultySet.BeatmapMode.Standard;
        var selected = new HashSet<BeatMapDifficultyInfo.Difficulty>();

        foreach (var settings in difficulties)
        {
            selected.Add(settings.Difficulty);

            var existing = beatmapInfo.FindDifficulty(settings.Difficulty, mode);
            if (existing is null)
            {
                NewDifficulty(beatmapInfo, mode, settings.Difficulty, settings.Njs, settings.NoteJumpStartBeatOffset);
                continue;
            }

            existing.Njs = settings.Njs;
            existing.NoteJumpStartBeatOffset = settings.NoteJumpStartBeatOffset;
            existing.Bpm = bpm;
            existing.Initialize();
        }

        foreach (var difficulty in Enum.GetValues<BeatMapDifficultyInfo.Difficulty>())
        {
            if (selected.Contains(difficulty))
            {
                continue;
            }

            var existing = beatmapInfo.FindDifficulty(difficulty, mode);
            if (existing is null)
            {
                continue;
            }

            var difficultyFilePath = beatmapInfo.MapFolder.PathJoin(existing.BeatMapFileName);
            if (FileAccess.FileExists(difficultyFilePath))
            {
                DirAccess.RemoveAbsolute(difficultyFilePath);
            }

            beatmapInfo.RemoveDifficulty(existing, mode);
        }

        beatmapInfo.SyncDifficultySets();
        SaveBeatmapInfo(beatmapInfo);
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
        var difficultyFilePath = CurrentBeatmapInfo.MapFolder.PathJoin(difficulty.BeatMapFileName);
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
        var infoPath = beatmapInfo.FilePath;
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