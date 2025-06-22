extends Node

signal current_beatmap_changed(beatmap: Variant)

var current_beatmap: Variant

func load_beatmap(file_path: String) -> void:
	var beatmap_file = FileAccess.open(file_path, FileAccess.READ)
	var beatmap_json = beatmap_file.get_as_text()
	
	var json: JSON = JSON.new()
	var result = json.parse(beatmap_json)
	
	assert(result == OK, "JSON Parse Error: %s in %s at line %s" % [json.get_error_message(), file_path, json.get_error_line()])
	
	current_beatmap = json.data
	
	current_beatmap_changed.emit(current_beatmap)
