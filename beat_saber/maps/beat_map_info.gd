class_name BeatMapInfo

var song_name: String
var song_sub_name: String
var song_author_name: String
var song_file_name: String
var bpm: float

var difficulty_beat_map_sets: Array[BeatMapDifficultySet] = []

func _init(original: Variant) -> void:
    assert(str(original._version).begins_with("2"), "Map version is not supported")

    song_name = original._songName
    song_sub_name = original._songSubName
    song_author_name = original._songAuthorName
    song_file_name = original._songFilename
    bpm = original._beatsPerMinute

    for difficulty_set in original._difficultyBeatmapSets:
        difficulty_beat_map_sets.append(BeatMapDifficultySet.from_v2_object(difficulty_set, bpm))
