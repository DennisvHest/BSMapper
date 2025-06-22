extends Node3D

var xr_interface: XRInterface

func _ready() -> void:
	xr_interface = XRServer.find_interface("OpenXR")
	
	if xr_interface and xr_interface.is_initialized():
		print("OpenXR initialized successfully")
		
		# OpenXR handles sync on its own.
		# Leaving the vsync mode enabled would limit the VR refresh rate to the refresh rate of the monitor, not the refresh rate of the VR hedset.
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
		
		get_viewport().use_xr = true
	else:
		print("OpenXR not initialized, please check if your headset is connected")
	
	BeatMapManager.load_beatmap("res://test_beatmaps/1a605 (Devil Town - Emilia)/ExpertPlusStandard.dat")

func _on_right_hand_button_pressed(name: String) -> void:
	print("Right hand button pressed %s" % name)
	
	if name == "ax_button":
		get_tree().reload_current_scene()
