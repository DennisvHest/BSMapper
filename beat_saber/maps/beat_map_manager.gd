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

func new_map(song_path: String) -> void:
	var test_map_folder = wip_beatmap_location.path_join("TEST_MAP_BS_MAPPER")

	var dir = DirAccess.open(test_map_folder.get_base_dir())
	if dir == null:
		dir = DirAccess.open("res://")
	var mk_err = dir.make_dir_recursive(test_map_folder)
	assert(mk_err == OK, "Failed to create directory: %s" % test_map_folder)

	# Copy the song file and rename to song.egg
	var song_dest_path = test_map_folder.path_join("song.egg")
	var src_file = FileAccess.open(song_path, FileAccess.READ)
	assert(src_file != null, "Failed to open source song file: %s" % song_path)
	var song_data = src_file.get_buffer(src_file.get_length())
	src_file.close()
	var dest_file = FileAccess.open(song_dest_path, FileAccess.WRITE)
	assert(dest_file != null, "Failed to open destination song file: %s" % song_dest_path)
	dest_file.store_buffer(song_data)
	dest_file.close()

	# Create BeatMapInfo and save original_object as info.dat
	var beatmap_info = BeatMapInfo.new_map(test_map_folder)
	var info_dat_path = test_map_folder.path_join("info.dat")
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
