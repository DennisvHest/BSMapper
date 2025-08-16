class_name BeatMapDifficultySet

var beat_map_characteristic_name: String
var difficulty_beat_maps: Array[BeatMapDifficultyInfo] = []

static func from_v2_object(original: Variant, bpm: float) -> BeatMapDifficultySet:
    var beat_map_set = BeatMapDifficultySet.new()

    beat_map_set.beat_map_characteristic_name = original._beatmapCharacteristicName

    for difficulty in original._difficultyBeatmaps:
        beat_map_set.difficulty_beat_maps.append(BeatMapDifficultyInfo.from_v2_object(difficulty, bpm))

    return beat_map_set
