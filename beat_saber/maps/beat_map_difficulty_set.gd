class_name BeatMapDifficultySet

enum BeatmapMode {
	STANDARD
}

var mode: BeatmapMode
var difficulty_beat_maps: Array[BeatMapDifficultyInfo] = []

func to_v2_object() -> Dictionary:
    var obj = {}
    obj["_beatmapCharacteristicName"] = get_mode_name(mode)
    obj["_difficultyBeatmaps"] = []
    for diff in difficulty_beat_maps:
        obj["_difficultyBeatmaps"].append(diff.to_v2_object())
    return obj

static func new_difficulty_set(mode: BeatmapMode) -> BeatMapDifficultySet:
    var beat_map_set = BeatMapDifficultySet.new()
    beat_map_set.mode = mode
    return beat_map_set

static func from_v2_object(original: Variant, bpm: float) -> BeatMapDifficultySet:
    var beat_map_set = BeatMapDifficultySet.new()

    beat_map_set.mode = get_mode(original._beatmapCharacteristicName)

    for difficulty in original._difficultyBeatmaps:
        beat_map_set.difficulty_beat_maps.append(BeatMapDifficultyInfo.from_v2_object(difficulty, bpm))

    return beat_map_set

static func get_mode(mode: String) -> BeatmapMode:
    match mode:
        "Standard":
            return BeatmapMode.STANDARD
        _:
            return BeatmapMode.STANDARD # TODO: Handle other modes

static func get_mode_name(mode: BeatmapMode) -> String:
    match mode:
        BeatmapMode.STANDARD:
            return "Standard"
        _:
            return "Standard" # TODO: Handle other modes