extends Node3D

class_name NoteBlock

enum NoteBlockType { LEFT = 0, RIGHT = 1 }

static var speed: float = 19

var velocity: Vector3 = Vector3.BACK * speed

var initial_position: Vector3
var type: NoteBlockType

func initialize(_initial_position: Vector3, note_block: Variant):
	initial_position = _initial_position
	position = initial_position
	
	var material: StandardMaterial3D = $MeshInstance3D.get_active_material(0)
	
	if note_block._type == NoteBlockType.LEFT:
		type = NoteBlockType.LEFT
		material.albedo_color = Color.RED
	elif note_block._type == NoteBlockType.RIGHT:
		type = NoteBlockType.RIGHT
		material.albedo_color = Color.BLUE
	
	set_cut_direction(note_block)

func set_cut_direction(note_block: Variant):
	var block_rotation = 0;
	
	match note_block._cutDirection:
		1.0: block_rotation = 180
		2.0: block_rotation = -90
		3.0: block_rotation = 90
		4.0: block_rotation = 45
		5.0: block_rotation = -45
		6.0: block_rotation = 135
		7.0: block_rotation = -135
		
	rotate_z(deg_to_rad(block_rotation));
	
	if note_block._cutDirection == 8.0:
		$CutDirectionTriangle.visible = false

func _process(delta: float) -> void:
	position = initial_position + velocity * PlaybackManager.playback_position

func _on_area_3d_area_entered(area: Area3D) -> void:
	if area.is_in_group("sabers"):
		GameEvents.note_block_hit.emit(type)
		queue_free()
