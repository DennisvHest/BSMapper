extends Node

signal current_beatmap_info_changed(beatmap_info: BeatMapInfo)
signal current_beatmap_difficulty_info_changed(difficulty: BeatMapDifficultyInfo)
signal current_beatmap_changed(beatmap: BeatMap)

var current_beatmap_info: BeatMapInfo
var current_beatmap_file_path: String
var current_beatmap: BeatMap

func load_beatmap_info(file_path: String) -> BeatMapInfo:
	var beatmap_info_json = FileAccess.get_file_as_string(file_path)
	
	var json: JSON = JSON.new()
	var result = json.parse(beatmap_info_json)
	
	assert(result == OK, "JSON Parse Error: %s in %s at line %s" % [json.get_error_message(), file_path, json.get_error_line()])

	current_beatmap_info = BeatMapInfo.new(json.data, file_path)
	current_beatmap_info_changed.emit(current_beatmap_info)

	return current_beatmap_info

func load_difficulty(difficulty: BeatMapDifficultyInfo) -> void:
	current_beatmap_difficulty_info_changed.emit(difficulty)
	var difficulty_file_path = current_beatmap_info.file_path.get_base_dir() + "/" + difficulty.beat_map_file_name
	_load_beatmap(difficulty_file_path)

func _load_beatmap(file_path: String) -> void:
	var beatmap_json = FileAccess.get_file_as_string(file_path)
	
	var json: JSON = JSON.new()
	var result = json.parse(beatmap_json)
	
	assert(result == OK, "JSON Parse Error: %s in %s at line %s" % [json.get_error_message(), file_path, json.get_error_line()])

	current_beatmap_file_path = file_path
	change_beatmap(BeatMap.new(json.data))

func change_beatmap(beatmap: BeatMap) -> void:
	current_beatmap = beatmap
	current_beatmap_changed.emit(current_beatmap)

func save_beatmap() -> void:
	current_beatmap.save_changes()

	var file_ext = current_beatmap_file_path.get_extension()
	var file_base = current_beatmap_file_path.get_basename()
	var new_file_path = "%s_TEST.%s" % [file_base, file_ext]

	var beamap_file_json = JSON.stringify(current_beatmap.original_map, "", false)

	var beatmap_file = FileAccess.open(new_file_path, FileAccess.WRITE)
	beatmap_file.store_string(beamap_file_json)
