extends Node3D

class_name NoteBlockLane

@export var note_block_scene: PackedScene
@export var bomb_scene: PackedScene
@export var wall_scene: PackedScene

@export var music: AudioStreamPlayer

# Width/height of the line in which the note block/bomb resides
const BEATMAP_OBJECT_LINE_SIZE: float = 0.5

const lane_width: float = BEATMAP_OBJECT_LINE_SIZE * 4
const lane_height: float = BEATMAP_OBJECT_LINE_SIZE * 3

func _ready() -> void:
	BeatMapManager.current_beatmap_changed.connect(_on_current_beatmap_changed)
	PlaybackManager.mode_changed.connect(_on_playback_mode_changed)

func _on_playback_mode_changed() -> void:
	# Jump animation should be disabled in editing mode, so that the note blocks don't jump around when editing
	for beatmap_object in get_children():
		if not beatmap_object is BeatmapObject:
			continue
		
		if PlaybackManager.mode == PlaybackManager.EditMode.EDITING:
			beatmap_object.set_jump_animation_enabled(false)
		else:
			beatmap_object.set_jump_animation_enabled(true)

func _on_current_beatmap_changed(current_beatmap: Variant):
	var map_info = BeatMapDifficultyInfo.new()
	
	for beatmap_object in current_beatmap._notes:
		var object_position = _get_beatmap_object_initial_position(beatmap_object, map_info)
		
		var beatmap_object_node: Node3D
		
		if beatmap_object._type == BeatmapObject.BeatmapObjectType.BOMB:
			var bomb_node: Bomb = bomb_scene.instantiate()
			bomb_node.initialize(object_position, map_info, beatmap_object)
			beatmap_object_node = bomb_node
		else:
			var note_block_node: NoteBlock = note_block_scene.instantiate()
			note_block_node.initialize(object_position, map_info, beatmap_object)
			beatmap_object_node = note_block_node
		
		add_child(beatmap_object_node)
		
	for wall in current_beatmap._obstacles:
		var wall_position = _get_beatmap_object_initial_position(wall, map_info)
		
		var wall_node: Wall = wall_scene.instantiate()
		wall_node.initialize(wall_position, map_info, wall)
		
		add_child(wall_node)

func _get_beatmap_object_initial_position(beatmap_object: Variant, map_info: BeatMapDifficultyInfo) -> Vector3:
	# How far in time (seconds) the object should be positioned initially using the BPM
	var hit_time: float = beatmap_object._time * 60 / map_info.bpm
	# Position the object (in meters) from the origin position of the note block lane -> forward direction -> using the speed of the note block
	var object_position: Vector3 = position + Vector3.FORWARD * map_info.njs * hit_time
	
	# Position object along the line index (horizontal) and line layer (vertical)
	object_position += Vector3.RIGHT * BEATMAP_OBJECT_LINE_SIZE * beatmap_object._lineIndex
	
	var line_layer: int
	if "_lineLayer"  in beatmap_object:
		line_layer = beatmap_object._lineLayer
	else:
		line_layer = 0
	
	object_position += Vector3.UP * BEATMAP_OBJECT_LINE_SIZE * line_layer
	
	# Center the note block lane horizontally
	object_position += Vector3.LEFT * lane_width / 2
	object_position += Vector3.RIGHT * BEATMAP_OBJECT_LINE_SIZE / 2
	
	# Move the note block lane up to the player height (the middle between the top and middle lane is at eye height)
	object_position += Vector3.UP * GlobalSettings.player_height * 1 / 3
	object_position += Vector3.UP * BEATMAP_OBJECT_LINE_SIZE / 2
	
	return object_position
