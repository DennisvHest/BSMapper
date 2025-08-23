extends Control

@export var start_scene: PackedScene

@export var debug_without_vr: bool = false

var xr_interface: XRInterface


func _ready() -> void:
	pass

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

func _on_install_location_button_pressed() -> void:
	$InstallLocationSelector/InstallLocationFolderDialog.show()

func _on_install_location_folder_dialog_dir_selected(dir: String) -> void:
	var wip_beatmaps_location = dir.path_join("Beat Saber_Data/CustomWIPLevels")
	BeatMapManager.set_wip_beatmap_location(wip_beatmaps_location)

	$InstallLocationSelector/InstallLocationLabel.text = wip_beatmaps_location

	$MapSelector.show()

func _on_new_map_button_pressed() -> void:
	$MapSelector/SongFileDialog.show()


func _on_song_file_dialog_file_selected(path: String) -> void:
	var new_beat_map = BeatMapManager.new_map(path)
	var new_difficulty = BeatMapManager.new_difficulty(new_beat_map, BeatMapDifficultySet.BeatmapMode.STANDARD, BeatMapDifficultyInfo.Difficulty.EXPERT, 16.0, -0.15)
