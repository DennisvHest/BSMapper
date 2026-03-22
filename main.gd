extends Control

@export var start_scene: PackedScene

@export var debug_without_vr: bool = false
@export var debug_start_in_editor: bool = false
@export_global_dir var beat_saber_install_location: String = ""
@export_file("*.ogg", "*.egg") var default_song_file: String = ""

var xr_interface: XRInterface


func _ready() -> void:
	_apply_exported_configuration()

	if debug_start_in_editor and _load_default_map():
		_start()

func _start() -> void:
	xr_interface = XRServer.find_interface("OpenXR")
	
	if !debug_without_vr and xr_interface and xr_interface.is_initialized():
		print("OpenXR initialized successfully")
		
		# OpenXR handles sync on its own.
		# Leaving the vsync mode enabled would limit the VR refresh rate to the refresh rate of the monitor, not the refresh rate of the VR hedset.
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
		
		get_viewport().use_xr = true
	else:
		print("OpenXR not initialized, please check if your headset is connected")
	
	get_tree().change_scene_to_packed(start_scene)

func _apply_exported_configuration() -> void:
	if !beat_saber_install_location.is_empty():
		$InstallLocationSelector/InstallLocationFolderDialog.current_dir = beat_saber_install_location

		var wip_beatmaps_location = _get_wip_beatmaps_location(beat_saber_install_location)
		if DirAccess.dir_exists_absolute(wip_beatmaps_location):
			_configure_install_location(beat_saber_install_location)
		else:
			push_warning("Configured Beat Saber install location does not contain Beat Saber_Data/CustomWIPLevels: %s" % beat_saber_install_location)

	if !default_song_file.is_empty():
		$MapSelector/SongFileDialog.current_dir = default_song_file.get_base_dir()
		$MapSelector/SongFileDialog.current_file = default_song_file.get_file()

func _get_wip_beatmaps_location(install_location: String) -> String:
	return install_location.path_join("Beat Saber_Data/CustomWIPLevels")

func _configure_install_location(dir: String) -> void:
	beat_saber_install_location = dir

	var wip_beatmaps_location = _get_wip_beatmaps_location(dir)
	BeatMapManager.set_wip_beatmap_location(wip_beatmaps_location)

	$InstallLocationSelector/InstallLocationLabel.text = wip_beatmaps_location
	$MapSelector.show()

func _load_default_map() -> bool:
	if beat_saber_install_location.is_empty():
		push_warning("Debug start is enabled, but no Beat Saber install location is configured on the main node.")
		return false

	if !DirAccess.dir_exists_absolute(beat_saber_install_location):
		push_warning("Configured Beat Saber install location does not exist: %s" % beat_saber_install_location)
		return false

	var wip_beatmaps_location = _get_wip_beatmaps_location(beat_saber_install_location)
	if !DirAccess.dir_exists_absolute(wip_beatmaps_location):
		push_warning("Configured Beat Saber install location is missing Beat Saber_Data/CustomWIPLevels: %s" % beat_saber_install_location)
		return false

	if default_song_file.is_empty():
		push_warning("Debug start is enabled, but no default song file is configured on the main node.")
		return false

	if !FileAccess.file_exists(default_song_file):
		push_warning("Configured default song file does not exist: %s" % default_song_file)
		return false

	_configure_install_location(beat_saber_install_location)
	return _load_selected_song(default_song_file)

func _load_selected_song(path: String) -> bool:
	if BeatMapManager.wip_beatmap_location.is_empty():
		push_warning("Cannot create a map before a Beat Saber install location is configured.")
		return false

	default_song_file = path

	var new_beat_map = BeatMapManager.new_map(path)
	BeatMapManager.new_difficulty(new_beat_map, BeatMapDifficultySet.BeatmapMode.STANDARD, BeatMapDifficultyInfo.Difficulty.EXPERT, 16.0, -0.15)

	var beatmap_info = BeatMapManager.load_beatmap_info(new_beat_map.file_path.path_join("info.dat"))

	var difficulty: BeatMapDifficultyInfo = beatmap_info.difficulty_beat_map_sets[0].difficulty_beat_maps[0]
	BeatMapManager.load_difficulty(difficulty)

	return true

func _on_install_location_button_pressed() -> void:
	$InstallLocationSelector/InstallLocationFolderDialog.show()

func _on_install_location_folder_dialog_dir_selected(dir: String) -> void:
	_configure_install_location(dir)

func _on_new_map_button_pressed() -> void:
	$MapSelector/SongFileDialog.show()


func _on_song_file_dialog_file_selected(path: String) -> void:
	if _load_selected_song(path):
		_start()
