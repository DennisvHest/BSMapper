using System;
using System.IO;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

[GlobalClass]
public partial class BeatMapInfo : RefCounted
{
    [Export]
    public string FilePath { get; set; } = string.Empty;

    [Export]
    public string SongName { get; set; } = string.Empty;

    [Export]
    public string SongSubName { get; set; } = string.Empty;

    [Export]
    public string SongAuthorName { get; set; } = string.Empty;

    [Export]
    public string SongFileName { get; set; } = string.Empty;

    /// <summary>Folder containing the map's info.dat, song and difficulty files.</summary>
    public string MapFolder => Path.GetDirectoryName(FilePath);

    public string SongFilePath => Path.Combine(MapFolder, SongFileName);

    [Export]
    public float Bpm { get; set; }

    [Export]
    public Variant OriginalObject { get; set; }

    [Export]
    public Array<BeatMapDifficultySet> DifficultyBeatMapSets { get; set; } = new();

    public static BeatMapInfo NewMap(
        string mapFolder,
        string songName,
        string songSubName,
        string songAuthorName,
        string songFileName,
        float bpm)
    {
        var data = new Dictionary
        {
            ["_version"] = "2.0.0",
            ["_songName"] = songName,
            ["_songSubName"] = songSubName,
            ["_songAuthorName"] = songAuthorName,
            ["_songFilename"] = songFileName,
            ["_beatsPerMinute"] = bpm,
            ["_difficultyBeatmapSets"] = new Array(),
        };
        return FromFile(data, mapFolder.PathJoin("info.dat"));
    }

    public static BeatMapInfo FromFile(Variant original, string filePath)
    {
        var data = original.AsGodotDictionary();
        var version = data["_version"].AsString();
        if (!version.StartsWith('2'))
        {
            throw new ArgumentException("Map version is not supported", nameof(original));
        }

        var info = new BeatMapInfo
        {
            OriginalObject = original,
            FilePath = filePath,
            SongName = data["_songName"].AsString(),
            SongSubName = data["_songSubName"].AsString(),
            SongAuthorName = data["_songAuthorName"].AsString(),
            SongFileName = data["_songFilename"].AsString(),
            Bpm = (float)data["_beatsPerMinute"].AsDouble(),
        };

        foreach (var difficultySet in data["_difficultyBeatmapSets"].AsGodotArray())
        {
            info.DifficultyBeatMapSets.Add(BeatMapDifficultySet.FromV2Object(difficultySet, info.Bpm));
        }

        return info;
    }

    public void UpdateSongInfo(string songName, string songSubName, string songAuthorName, float bpm)
    {
        SongName = songName;
        SongSubName = songSubName;
        SongAuthorName = songAuthorName;

        var bpmChanged = !Mathf.IsEqualApprox(Bpm, bpm);
        Bpm = bpm;

        var data = OriginalObject.AsGodotDictionary();
        data["_songName"] = songName;
        data["_songSubName"] = songSubName;
        data["_songAuthorName"] = songAuthorName;
        data["_beatsPerMinute"] = bpm;

        if (!bpmChanged)
        {
            return;
        }

        foreach (var set in DifficultyBeatMapSets)
        {
            foreach (var difficulty in set.DifficultyBeatMaps)
            {
                difficulty.Bpm = bpm;
                difficulty.Initialize();
            }
        }
    }

    public BeatMapDifficultyInfo FindDifficulty(
        BeatMapDifficultyInfo.Difficulty difficulty,
        BeatMapDifficultySet.BeatmapMode mode)
    {
        foreach (var set in DifficultyBeatMapSets)
        {
            if (set.Mode != mode)
            {
                continue;
            }

            foreach (var existing in set.DifficultyBeatMaps)
            {
                if (existing.DifficultyLevel == difficulty)
                {
                    return existing;
                }
            }
        }

        return null;
    }

    public void AddDifficulty(BeatMapDifficultyInfo difficulty, BeatMapDifficultySet.BeatmapMode mode)
    {
        BeatMapDifficultySet targetSet = null;
        foreach (var set in DifficultyBeatMapSets)
        {
            if (set.Mode == mode)
            {
                targetSet = set;
                break;
            }
        }

        if (targetSet is null)
        {
            targetSet = BeatMapDifficultySet.NewDifficultySet(mode);
            DifficultyBeatMapSets.Add(targetSet);
        }

        targetSet.DifficultyBeatMaps.Add(difficulty);
        SyncDifficultySets();
    }

    public void RemoveDifficulty(BeatMapDifficultyInfo difficulty, BeatMapDifficultySet.BeatmapMode mode)
    {
        for (var i = DifficultyBeatMapSets.Count - 1; i >= 0; i--)
        {
            var set = DifficultyBeatMapSets[i];
            if (set.Mode != mode)
            {
                continue;
            }

            set.DifficultyBeatMaps.Remove(difficulty);
            if (set.DifficultyBeatMaps.Count == 0)
            {
                DifficultyBeatMapSets.RemoveAt(i);
            }
        }

        SyncDifficultySets();
    }

    /// <summary>Rewrites the raw difficulty set data so it matches the parsed model.</summary>
    public void SyncDifficultySets()
    {
        var sets = new Array();
        foreach (var set in DifficultyBeatMapSets)
        {
            sets.Add(set.ToV2Object());
        }

        OriginalObject.AsGodotDictionary()["_difficultyBeatmapSets"] = sets;
    }
}