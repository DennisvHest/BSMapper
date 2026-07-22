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

    public string SongFilePath => Path.Combine(Path.GetDirectoryName(FilePath), SongFileName);

    [Export]
    public float Bpm { get; set; }

    [Export]
    public Variant OriginalObject { get; set; }

    [Export]
    public Array<BeatMapDifficultySet> DifficultyBeatMapSets { get; set; } = new();

    public static BeatMapInfo NewMap(string filePath)
    {
        var data = new Dictionary
        {
            ["_version"] = "2.0.0",
            ["_songName"] = "TEST_MAP_BS_MAPPER",
            ["_songSubName"] = string.Empty,
            ["_songAuthorName"] = "BSMapper",
            ["_songFilename"] = "song.egg",
            ["_beatsPerMinute"] = 175,
            ["_difficultyBeatmapSets"] = new Array(),
        };
        return FromFile(data, filePath);
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
        var data = OriginalObject.AsGodotDictionary();
        if (data.ContainsKey("_difficultyBeatmapSets"))
        {
            data["_difficultyBeatmapSets"].AsGodotArray().Add(targetSet.ToV2Object());
        }
    }
}