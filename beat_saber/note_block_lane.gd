extends Node3D

class_name NoteBlockLane

@export var note_block_scene: PackedScene
@export var bomb_scene: PackedScene
@export var wall_scene: PackedScene

@export var music: AudioStreamPlayer

# Width/height of the line in which the note block/bomb resides
const BEATMAP_OBJECT_LINE_SIZE: float = 0.5

const GRID_WIDTH: int = 4
const GRID_HEIGHT: int = 3

const LANE_WIDTH: float = BEATMAP_OBJECT_LINE_SIZE * GRID_WIDTH
const LANE_HEIGHT: float = BEATMAP_OBJECT_LINE_SIZE * GRID_HEIGHT

func _ready() -> void:
	BeatMapManager.current_beatmap_changed.connect(_on_current_beatmap_changed)


func _on_current_beatmap_changed(current_beatmap: BeatMap) -> void:
	clear_objects()

	var map_info = BeatMapDifficultyInfo.new()
	
	for note in current_beatmap.notes:
		var object_position = _get_beatmap_object_initial_position(note, map_info)

		var note_block_node: NoteBlock = note_block_scene.instantiate()
		note_block_node.initialize_note(object_position, map_info, note)

		add_child(note_block_node)
	
	for bomb in current_beatmap.bombs:
		var object_position = _get_beatmap_object_initial_position(bomb, map_info)

		var bomb_node: Bomb = bomb_scene.instantiate()
		bomb_node.initialize(object_position, map_info, bomb)

		add_child(bomb_node)
	
	for wall in current_beatmap.walls:
		var wall_position = _get_beatmap_object_initial_position(wall, map_info)
		
		var wall_node: Wall = wall_scene.instantiate()
		wall_node.initialize_wall(wall_position, map_info, wall)
		
		add_child(wall_node)

func _get_beatmap_object_initial_position(beatmap_object: BeatMapObjectBase, map_info: BeatMapDifficultyInfo) -> Vector3:
	# How far in time (seconds) the object should be positioned initially using the BPM
	var hit_time: float = beatmap_object.beat * 60 / map_info.bpm
	# Position the object (in meters) from the origin position of the note block lane -> forward direction -> using the speed of the note block
	var object_position: Vector3 = position + Vector3.FORWARD * map_info.njs * hit_time
	
	# Position object along the line index (horizontal) and line layer (vertical)
	object_position += Vector3.RIGHT * BEATMAP_OBJECT_LINE_SIZE * beatmap_object.line_index
	
	var line_layer: int
	if "line_layer"  in beatmap_object:
		line_layer = beatmap_object.line_layer
	else:
		line_layer = 0
	
	object_position += Vector3.UP * BEATMAP_OBJECT_LINE_SIZE * line_layer
	
	# Center the note block lane horizontally
	object_position += Vector3.LEFT * LANE_WIDTH / 2
	object_position += Vector3.RIGHT * BEATMAP_OBJECT_LINE_SIZE / 2
	
	# Move the note block lane up to the player height (the middle between the top and middle lane is at eye height)
	object_position += Vector3.UP * GlobalSettings.player_height * 1 / 3
	object_position += Vector3.UP * BEATMAP_OBJECT_LINE_SIZE / 2
	
	return object_position

func clear_objects() -> void:
	for child in get_children():
		if child is BeatmapObject:
			child.queue_free()
