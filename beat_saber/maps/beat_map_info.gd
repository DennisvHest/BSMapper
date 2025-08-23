class_name BeatMapInfo

var file_path: String

var song_name: String
var song_sub_name: String
var song_author_name: String
var song_file_name: String
var bpm: float

var original_object: Variant

var difficulty_beat_map_sets: Array[BeatMapDifficultySet] = []

static func new_map(_file_path: String) -> BeatMapInfo:
    var info_object = {
        _version = "2.0.0",
        _songName = "TEST_MAP_BS_MAPPER",
        _songSubName = "",
        _songAuthorName = "BSMapper",
        _songFilename = "song.egg",
        _beatsPerMinute = 0,
        _difficultyBeatmapSets = []
    }

    return BeatMapInfo.new(info_object, _file_path)

static func from_file(original: Variant, _file_path: String) -> BeatMapInfo:
    return BeatMapInfo.new(original, _file_path)

func _init(original: Variant, _file_path: String) -> void:
    assert(str(original._version).begins_with("2"), "Map version is not supported")

    original_object = original

    file_path = _file_path

    song_name = original._songName
    song_sub_name = original._songSubName
    song_author_name = original._songAuthorName
    song_file_name = original._songFilename
    bpm = original._beatsPerMinute

    for difficulty_set in original._difficultyBeatmapSets:
        difficulty_beat_map_sets.append(BeatMapDifficultySet.from_v2_object(difficulty_set, bpm))
