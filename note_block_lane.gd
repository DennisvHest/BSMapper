extends Node3D

@export var note_block_scene: PackedScene

static var bpm: int = 100
# @export_file("*.dat") var beatmap_file: String

func _ready() -> void:
	BeatMapManager.current_beatmap_changed.connect(_on_current_beatmap_changed)
	
func _on_current_beatmap_changed(current_beatmap: Variant):
	for note_block in current_beatmap._notes:
		var hit_time: float = note_block._time * 60 / bpm
		var note_block_position: Vector3 = position + Vector3.BACK * NoteBlock.speed * hit_time
		
		var note_block_node: NoteBlock = note_block_scene.instantiate()
		note_block_node.initialize(note_block_position)
		
		add_child(note_block_node)
