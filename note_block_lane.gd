extends Node3D

@export var note_block_scene: PackedScene
@export var music: AudioStreamPlayer

@export var lane_width: float = 4
@export var lane_height: float = 3

func _ready() -> void:
	BeatMapManager.current_beatmap_changed.connect(_on_current_beatmap_changed)
	
func _on_current_beatmap_changed(current_beatmap: Variant):
	var map_info = BeatMapDifficultyInfo.new()
	
	for note_block in current_beatmap._notes:
		# How far in time (seconds) the note should be positioned initially using the BPM
		var hit_time: float = note_block._time * 60 / map_info.bpm
		# Position the note block (in meters) from the origin position of the note block lane -> forward direction -> using the speed of the note block
		var note_block_position: Vector3 = position + Vector3.FORWARD * map_info.njs * hit_time
		
		# Position note block along the line index (horizontal) and line layer (vertical)
		var note_block_line_width = lane_width / 4
		var note_block_line_height = lane_height / 3
		
		note_block_position += Vector3.RIGHT * note_block_line_width * note_block._lineIndex
		note_block_position += Vector3.UP * note_block_line_height * note_block._lineLayer
		
		# Center the note block lane horizontally
		note_block_position += Vector3.LEFT * lane_width / 2
		note_block_position += Vector3.RIGHT * note_block_line_width / 2
		
		# Move the note block lane up to the player height (the middle between the top and middle lane is at eye height)
		note_block_position += Vector3.UP * GlobalSettings.player_height * 1 / 3
		note_block_position += Vector3.UP * 0.2 # Move up by half the note block height
		
		var note_block_node: NoteBlock = note_block_scene.instantiate()
		note_block_node.initialize(note_block_position, map_info, note_block)
		
		add_child(note_block_node)
