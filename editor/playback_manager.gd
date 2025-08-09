extends Node

signal mode_changed

var progress_bar: HSlider
var leftHand: XRController3D
var music: AudioStreamPlayer

var beatmap = BeatMapDifficultyInfo.new()

## The current playback position of the song in seconds
var playback_position: float = 0

## The current playback beat of the song
var playback_beat: float:
	get:
		if beatmap.bpm == 0:
			return 0.0
		
		return playback_position / (60.0 / beatmap.bpm)

const PLAYBACK_SCRUB_VELOCITY: float = 0.01

enum EditMode { PLAYING, EDITING }

var mode: EditMode = EditMode.PLAYING

func _ready() -> void:
	# TODO: remove this HACK: check the main node to see if we are running the application or just a scene
	if not get_parent().has_node("Main"):
		process_mode = ProcessMode.PROCESS_MODE_DISABLED
		return

	progress_bar = get_parent().get_node("Main/DebugUI/MusicProgressBar")
	progress_bar.drag_started.connect(_on_music_progress_bar_drag_started)
	progress_bar.drag_ended.connect(_on_music_progress_bar_drag_ended)

	leftHand = get_parent().get_node("Main/XROrigin3D/LeftHand")
	leftHand.input_vector2_changed.connect(_on_left_hand_input_vector_2_changed)
	leftHand.button_pressed.connect(_on_left_hand_button_pressed)

	music = AudioStreamPlayer.new()
	music.stream = preload("res://test_beatmaps/1feab (Turn It Up - abcbadq)/song.ogg")
	music.volume_db = -10

	get_parent().add_child.call_deferred(music)

func play(from_position: float = 0):
	playback_position = from_position

	if mode == EditMode.PLAYING:
		music.play(from_position)

func pause():
	music.stream_paused = true

func _physics_process(delta: float) -> void:
	var leftJoystickPosition: Vector2 = leftHand.get_vector2("primary")
	
	if leftJoystickPosition.x != 0.0:
		progress_bar.value += + leftJoystickPosition.x * PLAYBACK_SCRUB_VELOCITY * delta;

func _process(delta: float) -> void:
	if music.stream_paused:
		PlaybackManager.playback_position = get_playback_position()
	else:
		PlaybackManager.playback_position = music.get_playback_position() + AudioServer.get_time_since_last_mix()

	var music_stream: AudioStream = music.stream
	
	progress_bar.value = PlaybackManager.playback_position / music_stream.get_length()

func _on_music_progress_bar_drag_started() -> void:
	music.stream_paused = true

func _on_music_progress_bar_drag_ended(value_changed: bool) -> void:
	if !value_changed:
		return

	music.play(get_playback_position())

func get_playback_position() -> float:
	var music_stream: AudioStream = music.stream
	return music_stream.get_length() * progress_bar.value

func change_mode(new_mode: EditMode) -> void:
	if mode == new_mode:
		return
	
	mode = new_mode

	if mode == EditMode.PLAYING:
		play(get_playback_position())
		print("Playback started")
	else:
		pause()
		print("Playback paused")

	mode_changed.emit()


func _on_left_hand_input_vector_2_changed(name: String, value: Vector2) -> void:
	if name != "primary":
		return

	if value.x != 0.0:
		pause()
	else:
		# Snap to nearest beat based on BPM
		var beat_duration = 60.0 / beatmap.bpm
		var playback_pos = get_playback_position()
		var snapped_pos = round(playback_pos / beat_duration) * beat_duration

		# Set progress bar to snapped position
		var music_stream: AudioStream = music.stream
		progress_bar.value = snapped_pos / music_stream.get_length()

		play(snapped_pos)

func _on_left_hand_button_pressed(button_name: String) -> void:
	print("Left hand button pressed %s" % button_name)
	if button_name == "ax_button":
		if mode == EditMode.EDITING:
			change_mode(EditMode.PLAYING)
		else:
			change_mode(EditMode.EDITING)
