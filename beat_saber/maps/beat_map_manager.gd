extends Node

signal current_beatmap_info_changed(beatmap_info: BeatMapInfo)
signal current_beatmap_difficulty_info_changed(difficulty: BeatMapDifficultyInfo)
signal current_beatmap_changed(beatmap: BeatMap)

var current_beatmap_info: BeatMapInfo
var current_beatmap_file_path: String
var current_beatmap: BeatMap

var wip_beatmap_location: String

func set_wip_beatmap_location(file_path: String) -> void:
	wip_beatmap_location = file_path

func new_map(song_path: String) -> BeatMapInfo:
	var test_map_folder = wip_beatmap_location.path_join("TEST_MAP_BS_MAPPER")

	var dir = DirAccess.open(wip_beatmap_location)
	dir.make_dir_recursive(test_map_folder)

	# Copy song for the map to the map files
	var song_destination_path = test_map_folder.path_join("song.egg")
	var src_file = FileAccess.open(song_path, FileAccess.READ)
	assert(src_file != null, "Failed to open source song file: %s" % song_path)
	var song_data = src_file.get_buffer(src_file.get_length())
	src_file.close()

	var dest_file = FileAccess.open(song_destination_path, FileAccess.WRITE)
	assert(dest_file != null, "Failed to open destination song file: %s" % song_destination_path)
	dest_file.store_buffer(song_data)
	dest_file.close()

	var beatmap_info = BeatMapInfo.new_map(test_map_folder)
	_save_beatmap_info(beatmap_info)

	return beatmap_info

func new_difficulty(beatmap_info: BeatMapInfo, mode: BeatMapDifficultySet.BeatmapMode, difficulty: BeatMapDifficultyInfo.Difficulty, njs: float, note_jump_start_beat_offset: float) -> BeatMapDifficultyInfo:
	var difficulty_info = BeatMapDifficultyInfo.new_difficulty(difficulty, mode, njs, note_jump_start_beat_offset, beatmap_info.bpm)

	var difficulty_file_path = beatmap_info.file_path.path_join(difficulty_info.beat_map_file_name)
	assert(!FileAccess.file_exists(difficulty_file_path), "Difficulty file already exists: %s" % difficulty_file_path)

	var beatmap = BeatMap.new_empty()
	var beatmap_json = JSON.stringify(beatmap.original_map, "", false)

	var difficulty_file = FileAccess.open(difficulty_file_path, FileAccess.WRITE)
	assert(difficulty_file != null, "Failed to open difficulty file for writing: %s" % difficulty_file_path)
	difficulty_file.store_string(beatmap_json)
	difficulty_file.close()

	beatmap_info.add_difficulty(difficulty_info, mode)
	_save_beatmap_info(beatmap_info)

	return difficulty_info

func _save_beatmap_info(beatmap_info: BeatMapInfo) -> void:
	var info_dat_path = beatmap_info.file_path.path_join("info.dat")
	var info_json = JSON.stringify(beatmap_info.original_object, "", false)
	var info_file = FileAccess.open(info_dat_path, FileAccess.WRITE)
	assert(info_file != null, "Failed to open info.dat for writing: %s" % info_dat_path)
	info_file.store_string(info_json)
	info_file.close()

func load_beatmap_info(file_path: String) -> BeatMapInfo:
	var beatmap_info_json = FileAccess.get_file_as_string(file_path)
	
	var json: JSON = JSON.new()
	var result = json.parse(beatmap_info_json)
	
	assert(result == OK, "JSON Parse Error: %s in %s at line %s" % [json.get_error_message(), file_path, json.get_error_line()])

	current_beatmap_info = BeatMapInfo.from_file(json.data, file_path)
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
	change_beatmap(BeatMap.from_file(json.data))

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
