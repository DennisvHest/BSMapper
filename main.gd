extends Node3D

@export var debug_without_vr: bool = false
@export_file("*.dat") var beatmap_file_path: String

var xr_interface: XRInterface

func _ready() -> void:
	xr_interface = XRServer.find_interface("OpenXR")
	
	if !debug_without_vr and xr_interface and xr_interface.is_initialized():
		print("OpenXR initialized successfully")
		
		# OpenXR handles sync on its own.
		# Leaving the vsync mode enabled would limit the VR refresh rate to the refresh rate of the monitor, not the refresh rate of the VR hedset.
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
		
		get_viewport().use_xr = true
	else:
		print("OpenXR not initialized, please check if your headset is connected")
	
	$XROrigin3D/XRCamera3D.position.y = GlobalSettings.player_height
	
	GameEvents.note_block_hit.connect(_on_note_block_hit)
	
	BeatMapManager.load_beatmap(beatmap_file_path)
	$Music.play()

func _on_right_hand_button_pressed(button_name: String) -> void:
	print("Right hand button pressed %s" % button_name)
	
	if button_name == "ax_button":
		get_tree().reload_current_scene()

func _on_note_block_hit():
	$HitSound.play()
