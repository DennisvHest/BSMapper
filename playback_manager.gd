extends Node

class_name PlaybackManager

static var playback_position: float = 0

func play(from_position: float = 0):
	$Music.play(from_position)

func _process(delta: float) -> void:
	if $Music.stream_paused:
		return
	
	playback_position = $Music.get_playback_position() + AudioServer.get_time_since_last_mix()

func _on_music_progress_bar_drag_started() -> void:
	$Music.stream_paused = true

func _on_music_progress_bar_drag_ended(value_changed: bool) -> void:
	if !value_changed:
		return
	
	var music_stream: AudioStream = $Music.stream
	var progres_bar: HSlider = get_parent().get_node("DebugUI/MusicProgressBar")
	
	var new_position = music_stream.get_length() * progres_bar.value
	
	$Music.play(new_position)
