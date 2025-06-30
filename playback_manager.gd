extends Node

class_name PlaybackManager

var progress_bar: HSlider

static var playback_position: float = 0

func _ready() -> void:
	progress_bar = get_parent().get_node("DebugUI/MusicProgressBar");

func play(from_position: float = 0):
	$Music.play(from_position)

func _process(delta: float) -> void:
	if $Music.stream_paused:
		playback_position = get_playback_position()
	else:
		playback_position = $Music.get_playback_position() + AudioServer.get_time_since_last_mix()

func _on_music_progress_bar_drag_started() -> void:
	$Music.stream_paused = true

func _on_music_progress_bar_drag_ended(value_changed: bool) -> void:
	if !value_changed:
		return
	
	$Music.play(get_playback_position())

func get_playback_position() -> float:
	var music_stream: AudioStream = $Music.stream
	return music_stream.get_length() * progress_bar.value
