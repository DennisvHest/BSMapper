extends Node3D

@export var note_block_scene: PackedScene

@export var lane_width: float = 4
@export var lane_height: float = 3

@export var bpm: int = 175

func _ready() -> void:
	BeatMapManager.current_beatmap_changed.connect(_on_current_beatmap_changed)
	
func _on_current_beatmap_changed(current_beatmap: Variant):
	for note_block in current_beatmap._notes:
		# How far in time (seconds) the note should be positioned initially using the BPM
		var hit_time: float = note_block._time * 60 / bpm
		
		# Position the note block (in meters) from the origin position of the note block lane -> forward direction -> using the speed of the note block
		var note_block_position: Vector3 = position + Vector3.FORWARD * NoteBlock.speed * hit_time
		
		# Position note block along the line index (horizontal) and line layer (vertical)
		var note_block_line_width = lane_width / 4
		var note_block_line_height = lane_height / 3
		
		note_block_position += Vector3.RIGHT * note_block_line_width * note_block._lineIndex
		note_block_position += Vector3.UP * note_block_line_height * note_block._lineLayer
		
		# Center the note block lane horizontally and vertically
		note_block_position += Vector3.LEFT * lane_width / 2
		note_block_position += Vector3.DOWN * note_block_line_height / 2
		
		# move the note block lane up to the player height
		note_block_position += Vector3.UP * GlobalSettings.player_height / 2
		
		var note_block_node: NoteBlock = note_block_scene.instantiate()
		note_block_node.initialize(note_block_position)
		
		add_child(note_block_node)
