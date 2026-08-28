using Godot;
using Godot.Collections;
using System.Collections.Generic;

[GlobalClass]
public partial class BeatMapDifficultyInfo : RefCounted
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Expert,
        ExpertPlus,
    }

    public static readonly IEnumerable<Difficulty> AllDifficulties = new List<Difficulty>
    {
        Difficulty.Easy,
        Difficulty.Normal,
        Difficulty.Hard,
        Difficulty.Expert,
        Difficulty.ExpertPlus,
    };

    /// <summary>The default half jump distance is 4 beats away from the player.</summary>
    public const float DefaultHalfJumpDistance = 4.0f;

    /// <summary>
    /// Maximum full jump distance in meters. This is a community-accepted approximation of the
    /// internal value used in Beat Saber.
    /// </summary>
    public const float MaxJumpDistanceMeters = 35.998f;

    /// <summary>Minimum half jump distance to avoid the reaction time being too short.</summary>
    public const float MinHalfJumpDistance = 0.25f;

    private float _halfJumpDistance;
    private float _jumpDistanceMeters;
    private float _halfJumpDistanceMeters;

    [Export]
    public Difficulty DifficultyLevel { get; set; }

    [Export]
    public string BeatMapFileName { get; set; } = string.Empty;

    /// <summary>Note jump speed in meters per second.</summary>
    [Export]
    public float Njs { get; set; }

    /// <summary>
    /// Offset from <see cref="DefaultHalfJumpDistance"/>, in beats. Mappers use this to align note
    /// jumps with the rhythm of the song.
    /// </summary>
    [Export]
    public float NoteJumpStartBeatOffset { get; set; }

    /// <summary>Beats per minute of the song.</summary>
    [Export]
    public float Bpm { get; set; }

    /// <summary>
    /// The half jump distance, in beats, is where notes jump up before reaching their target
    /// position and speed toward the player.
    /// </summary>
    [Export]
    public float HalfJumpDistance
    {
        get => _halfJumpDistance;
        set
        {
            _halfJumpDistance = value;
            JumpDistanceMeters = GetJumpDistanceMeters(value, Bpm, Njs);
        }
    }

    /// <summary>Total jump distance in meters.</summary>
    [Export]
    public float JumpDistanceMeters
    {
        get => _jumpDistanceMeters;
        set
        {
            _jumpDistanceMeters = value;
            HalfJumpDistanceMeters = value / 2.0f;
        }
    }

    /// <summary>Half jump distance in meters.</summary>
    [Export]
    public float HalfJumpDistanceMeters
    {
        get => _halfJumpDistanceMeters;
        set
        {
            _halfJumpDistanceMeters = value;
            ReactionTime = value / Njs;
        }
    }

    /// <summary>Duration of one beat in seconds.</summary>
    [Export]
    public float BeatDuration { get; set; }

    /// <summary>
    /// Time in seconds from when a note jumps up until the player is expected to hit it.
    /// </summary>
    [Export]
    public float ReactionTime { get; set; }

    public void Initialize()
    {
        BeatDuration = Bpm == 0.0f ? 0.0f : 60.0f / Bpm;
        HalfJumpDistance = GetHalfJumpDistance(Bpm, Njs, NoteJumpStartBeatOffset);
        JumpDistanceMeters = GetJumpDistanceMeters(HalfJumpDistance, Bpm, Njs);
        ReactionTime = JumpDistanceMeters / 2.0f / Njs;
    }

    public Dictionary ToV2Object()
    {
        return new Dictionary
        {
            ["_difficulty"] = GetDifficultyName(DifficultyLevel),
            ["_beatmapFilename"] = BeatMapFileName,
            ["_noteJumpMovementSpeed"] = Njs,
            ["_noteJumpStartBeatOffset"] = NoteJumpStartBeatOffset,
        };
    }

    public static BeatMapDifficultyInfo NewDifficulty(
        Difficulty difficulty,
        BeatMapDifficultySet.BeatmapMode mode,
        float njs,
        float noteJumpStartBeatOffset,
        float bpm)
    {
        var info = new BeatMapDifficultyInfo
        {
            DifficultyLevel = difficulty,
            BeatMapFileName = GetFileName(difficulty, mode),
            Njs = njs,
            NoteJumpStartBeatOffset = noteJumpStartBeatOffset,
            Bpm = bpm,
        };
        info.Initialize();
        return info;
    }

    public static BeatMapDifficultyInfo FromV2Object(Variant original, float bpm)
    {
        var data = original.AsGodotDictionary();
        var info = new BeatMapDifficultyInfo
        {
            DifficultyLevel = GetDifficulty(data["_difficulty"].AsString()),
            BeatMapFileName = data["_beatmapFilename"].AsString(),
            Njs = (float)data["_noteJumpMovementSpeed"].AsDouble(),
            NoteJumpStartBeatOffset = (float)data["_noteJumpStartBeatOffset"].AsDouble(),
            Bpm = bpm,
        };
        info.Initialize();
        return info;
    }

    public static Difficulty GetDifficulty(string difficulty)
    {
        return difficulty switch
        {
            "Normal" => Difficulty.Normal,
            "Hard" => Difficulty.Hard,
            "Expert" => Difficulty.Expert,
            "ExpertPlus" => Difficulty.ExpertPlus,
            _ => Difficulty.Easy,
        };
    }

    public static string GetDifficultyName(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Normal => "Normal",
            Difficulty.Hard => "Hard",
            Difficulty.Expert => "Expert",
            Difficulty.ExpertPlus => "ExpertPlus",
            _ => "Easy",
        };
    }

    public static string GetDifficultyDisplayName(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Normal => "Normal",
            Difficulty.Hard => "Hard",
            Difficulty.Expert => "Expert",
            Difficulty.ExpertPlus => "Expert+",
            _ => "Easy",
        };
    }

    public static string GetFileName(Difficulty difficulty, BeatMapDifficultySet.BeatmapMode mode)
    {
        return $"{GetDifficultyName(difficulty)}{BeatMapDifficultySet.GetModeName(mode)}.dat";
    }

    /// <summary>
    /// Converts the given half jump distance in beats to the full jump distance in meters.
    /// </summary>
    private static float GetJumpDistanceMeters(float halfJumpDistance, float bpm, float njs)
    {
        var halfJumpDistanceSeconds = 60.0f / bpm * halfJumpDistance;
        return njs * 2.0f * halfJumpDistanceSeconds;
    }

    /// <summary>
    /// Calculates the map's half jump distance in beats and applies the note jump start beat
    /// offset. This mimics Beat Saber's clamping behavior, which prevents the jump distance
    /// (how far away notes spawn) from exceeding a threshold of approximately 36 meters.
    /// </summary>
    private static float GetHalfJumpDistance(float bpm, float njs, float noteJumpStartBeatOffset)
    {
        var halfJumpDistance = DefaultHalfJumpDistance;
        while (GetJumpDistanceMeters(halfJumpDistance, bpm, njs) > MaxJumpDistanceMeters)
        {
            halfJumpDistance /= 2.0f;
        }

        return Mathf.Max(halfJumpDistance + noteJumpStartBeatOffset, MinHalfJumpDistance);
    }
}