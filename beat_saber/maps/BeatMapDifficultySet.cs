using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BeatMapDifficultySet : RefCounted
{
    public enum BeatmapMode
    {
        Standard,
    }

    [Export]
    public BeatmapMode Mode { get; set; }

    [Export]
    public Array<BeatMapDifficultyInfo> DifficultyBeatMaps { get; set; } = new();

    public Dictionary ToV2Object()
    {
        var difficulties = new Array();
        foreach (var difficulty in DifficultyBeatMaps)
        {
            difficulties.Add(difficulty.ToV2Object());
        }

        return new Dictionary
        {
            ["_beatmapCharacteristicName"] = GetModeName(Mode),
            ["_difficultyBeatmaps"] = difficulties,
        };
    }

    public static BeatMapDifficultySet NewDifficultySet(BeatmapMode mode)
    {
        return new BeatMapDifficultySet { Mode = mode };
    }

    public static BeatMapDifficultySet FromV2Object(Variant original, float bpm)
    {
        var data = original.AsGodotDictionary();
        var set = new BeatMapDifficultySet
        {
            Mode = GetMode(data["_beatmapCharacteristicName"].AsString()),
        };

        foreach (var difficulty in data["_difficultyBeatmaps"].AsGodotArray())
        {
            set.DifficultyBeatMaps.Add(BeatMapDifficultyInfo.FromV2Object(difficulty, bpm));
        }

        return set;
    }

    public static BeatmapMode GetMode(string mode)
    {
        return BeatmapMode.Standard;
    }

    public static string GetModeName(BeatmapMode mode)
    {
        return "Standard";
    }
}