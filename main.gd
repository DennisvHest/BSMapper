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
	GameEvents.bomb_hit.connect(_on_bomb_hit)
	
	BeatMapManager.load_beatmap(beatmap_file_path)
	$PlaybackManager.play()

func _on_right_hand_button_pressed(button_name: String) -> void:
	print("Right hand button pressed %s" % button_name)
	
	if button_name == "ax_button":
		get_tree().reload_current_scene()

func _on_note_block_hit(saber_type):
	$HitSound.play(0.15) #: Hit sounds are played at an offset, otherwise it feels like the sound plays before the block is even hit
	_trigger_saber_haptic_pulse(saber_type)

func _on_bomb_hit(saber_type):
	$BadCutSound.play()
	_trigger_saber_haptic_pulse(saber_type)

func _trigger_saber_haptic_pulse(saber_type):
	if saber_type == Saber.SaberType.LEFT:
		$XROrigin3D/LeftHand.trigger_haptic_pulse("haptic", 0.0, 1.0, 0.15, 0.0)
	else:
		$XROrigin3D/RightHand.trigger_haptic_pulse("haptic", 0.0, 1.0, 0.15, 0.0)
