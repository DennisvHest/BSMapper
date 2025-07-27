extends Node

class_name PlaybackManager

var progress_bar: HSlider
var leftHand: XRController3D

static var playback_position: float = 0

const PLAYBACK_SCRUB_VELOCITY: float = 0.01

func _ready() -> void:
	progress_bar = get_parent().get_node("DebugUI/MusicProgressBar")
	leftHand = get_parent().get_node("XROrigin3D/LeftHand")

func play(from_position: float = 0):
	$Music.play(from_position)

func _physics_process(delta: float) -> void:
	var leftJoystickPosition: Vector2 = leftHand.get_vector2("primary")
	
	if leftJoystickPosition.x != 0.0:
		var progress_value = progress_bar.value + leftJoystickPosition.x * PLAYBACK_SCRUB_VELOCITY * delta;
		progress_bar.value = progress_value
		print(progress_bar.value)

func _process(delta: float) -> void:
	if $Music.stream_paused:
		PlaybackManager.playback_position = get_playback_position()
	else:
		PlaybackManager.playback_position = $Music.get_playback_position() + AudioServer.get_time_since_last_mix()
		
	var music_stream: AudioStream = $Music.stream
	
	progress_bar.value = PlaybackManager.playback_position / music_stream.get_length()

func _on_music_progress_bar_drag_started() -> void:
	$Music.stream_paused = true

func _on_music_progress_bar_drag_ended(value_changed: bool) -> void:
	if !value_changed:
		return
	
	$Music.play(get_playback_position())

func get_playback_position() -> float:
	var music_stream: AudioStream = $Music.stream
	return music_stream.get_length() * progress_bar.value


func _on_left_hand_input_vector_2_changed(name: String, value: Vector2) -> void:
	if name != "primary":
		return
	
	if value.x != 0.0:
		$Music.stream_paused = true
	else:
		$Music.play(get_playback_position())
