extends Node3D

class_name FloorBeatGrid

const FLOOR_MARKER_Y: float = 0.01
const LINE_HEIGHT: float = 0.002
const MAJOR_LINE_THICKNESS: float = 0.088
const MINOR_LINE_THICKNESS: float = 0.006
const LONGITUDINAL_LINE_THICKNESS: float = 0.01
const CURRENT_BEAT_LINE_THICKNESS: float = 0.088
const VISIBLE_BEATS_AHEAD: float = 8.0
const VISIBLE_BEATS_BEHIND: float = 2.0
const QUARTER_BEAT_STEP: float = 0.25

var floor_grid_root: Node3D
var current_beat_marker: MeshInstance3D
var line_mesh: BoxMesh

var major_line_material: StandardMaterial3D
var minor_line_material: StandardMaterial3D
var longitudinal_line_material: StandardMaterial3D
var current_beat_line_material: StandardMaterial3D
var rendered_window_start_quarter: int = -1
var rendered_window_end_quarter: int = -1

func _ready() -> void:
	_initialize_floor_grid()
	_on_playback_mode_changed()
	_rebuild_floor_grid()
	PlaybackManager.mode_changed.connect(_on_playback_mode_changed)
	BeatMapManager.current_beatmap_changed.connect(_on_current_beatmap_changed)
	BeatMapManager.current_beatmap_difficulty_info_changed.connect(_on_current_beatmap_difficulty_info_changed)
	_update_floor_grid_position()

func _process(_delta: float) -> void:
	_refresh_visible_window_if_needed()
	_update_floor_grid_position()

func _initialize_floor_grid() -> void:
	line_mesh = BoxMesh.new()

	major_line_material = _create_line_material(Color(1.0, 1.0, 1.0, 0.32))
	minor_line_material = _create_line_material(Color(1.0, 1.0, 1.0, 0.12))
	longitudinal_line_material = _create_line_material(Color(1.0, 1.0, 1.0, 0.18))
	current_beat_line_material = _create_line_material(Color(0.3, 0.8, 1.0, 0.75))

	floor_grid_root = Node3D.new()
	floor_grid_root.name = "FloorBeatGrid"
	add_child(floor_grid_root)

	current_beat_marker = _create_line(
		"CurrentBeatMarker",
		Vector3(NoteBlockLane.LANE_WIDTH, LINE_HEIGHT, CURRENT_BEAT_LINE_THICKNESS),
		current_beat_line_material
	)
	current_beat_marker.position = Vector3(0.0, FLOOR_MARKER_Y, 0.0)
	add_child(current_beat_marker)

func _create_line_material(color: Color) -> StandardMaterial3D:
	var material := StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.albedo_color = color
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	return material

func _create_line(line_name: String, line_scale: Vector3, material: Material) -> MeshInstance3D:
	var line := MeshInstance3D.new()
	line.name = line_name
	line.mesh = line_mesh
	line.material_override = material
	line.scale = line_scale
	line.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return line

func _rebuild_floor_grid() -> void:
	rendered_window_start_quarter = -1
	rendered_window_end_quarter = -1
	_refresh_visible_window_if_needed()

func _refresh_visible_window_if_needed() -> void:
	var difficulty := BeatMapManager.current_beatmap_difficulty_info
	if difficulty == null or difficulty.beat_duration == 0.0:
		_clear_floor_lines()
		current_beat_marker.hide()
		return

	current_beat_marker.show()

	var total_beats := _get_total_beats(difficulty)
	if total_beats <= 0.0:
		_clear_floor_lines()
		return

	var visible_window := _get_visible_window(total_beats)
	var window_start_quarter := int(round(visible_window.x / QUARTER_BEAT_STEP))
	var window_end_quarter := int(round(visible_window.y / QUARTER_BEAT_STEP))

	if window_start_quarter == rendered_window_start_quarter and window_end_quarter == rendered_window_end_quarter:
		return

	rendered_window_start_quarter = window_start_quarter
	rendered_window_end_quarter = window_end_quarter

	_clear_floor_lines()

	var beat_length_meters := difficulty.njs * difficulty.beat_duration

	_spawn_longitudinal_lines(visible_window.x, visible_window.y, beat_length_meters)
	_spawn_cross_lines(window_start_quarter, window_end_quarter, beat_length_meters)
	_update_floor_grid_position()

func _clear_floor_lines() -> void:
	for child in floor_grid_root.get_children():
		child.queue_free()

func _spawn_longitudinal_lines(window_start_beat: float, window_end_beat: float, beat_length_meters: float) -> void:
	var floor_center_beat := (window_start_beat + window_end_beat) / 2.0
	var floor_length_meters := (window_end_beat - window_start_beat) * beat_length_meters

	for line_index in NoteBlockLane.GRID_WIDTH + 1:
		var x_position := -NoteBlockLane.LANE_WIDTH / 2.0 + NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE * line_index
		var line := _create_line(
			"LaneBoundary%s" % line_index,
			Vector3(LONGITUDINAL_LINE_THICKNESS, LINE_HEIGHT, floor_length_meters),
			longitudinal_line_material
		)
		line.position = Vector3(x_position, FLOOR_MARKER_Y, -floor_center_beat * beat_length_meters)
		floor_grid_root.add_child(line)

func _spawn_cross_lines(window_start_quarter: int, window_end_quarter: int, beat_length_meters: float) -> void:
	for quarter_beat_index in range(window_start_quarter, window_end_quarter + 1):
		var is_major_line := quarter_beat_index % 4 == 0
		var line_thickness := MAJOR_LINE_THICKNESS if is_major_line else MINOR_LINE_THICKNESS
		var material := major_line_material if is_major_line else minor_line_material
		var beat_position := quarter_beat_index * QUARTER_BEAT_STEP
		var line := _create_line(
			"BeatMarker%s" % quarter_beat_index,
			Vector3(NoteBlockLane.LANE_WIDTH, LINE_HEIGHT, line_thickness),
			material
		)

		line.position = Vector3(0.0, FLOOR_MARKER_Y, -beat_position * beat_length_meters)
		floor_grid_root.add_child(line)

func _update_floor_grid_position() -> void:
	var difficulty := BeatMapManager.current_beatmap_difficulty_info
	if difficulty == null:
		return

	floor_grid_root.position = Vector3(0.0, 0.0, PlaybackManager.playback_position * difficulty.njs)

func _get_total_beats(difficulty: BeatMapDifficultyInfo) -> float:
	var last_beat := _get_music_total_beats(difficulty)
	var beatmap := BeatMapManager.current_beatmap

	if beatmap != null:
		for note in beatmap.notes:
			last_beat = max(last_beat, note.beat)

		for bomb in beatmap.bombs:
			last_beat = max(last_beat, bomb.beat)

		for wall in beatmap.walls:
			last_beat = max(last_beat, wall.beat + wall.duration)

	return ceil(last_beat)

func _get_music_total_beats(difficulty: BeatMapDifficultyInfo) -> float:
	if PlaybackManager.music == null or PlaybackManager.music.stream == null:
		return 0.0

	return PlaybackManager.music.stream.get_length() / difficulty.beat_duration

func _get_visible_window(total_beats: float) -> Vector2:
	var current_beat := PlaybackManager.playback_beat
	var start_beat: float = max(floor((current_beat - VISIBLE_BEATS_BEHIND) / QUARTER_BEAT_STEP) * QUARTER_BEAT_STEP, 0.0)
	var end_beat: float = min(ceil((current_beat + VISIBLE_BEATS_AHEAD) / QUARTER_BEAT_STEP) * QUARTER_BEAT_STEP, total_beats)
	return Vector2(start_beat, end_beat)

func _on_current_beatmap_changed(_beatmap: BeatMap) -> void:
	_rebuild_floor_grid()

func _on_current_beatmap_difficulty_info_changed(_difficulty: BeatMapDifficultyInfo) -> void:
	_rebuild_floor_grid()

func _on_playback_mode_changed() -> void:
	if PlaybackManager.mode == PlaybackManager.EditMode.EDITING:
		show()
	else:
		hide()
