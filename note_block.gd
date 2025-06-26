extends Node3D

class_name NoteBlock

static var speed: float = 19

func initialize(initial_position: Vector3, note_block: Variant):
	position = initial_position
	
	var material: StandardMaterial3D = $MeshInstance3D.get_active_material(0)
	
	if note_block._type == 0:
		material.albedo_color = Color.RED
	elif note_block._type == 1:
		material.albedo_color = Color.BLUE
	
	set_cut_direction(note_block)

func set_cut_direction(note_block: Variant):
	var rotation = 0;
	
	match note_block._cutDirection:
		1.0: rotation = 180
		2.0: rotation = -90
		3.0: rotation = 90
		4.0: rotation = 45
		5.0: rotation = -45
		6.0: rotation = 135
		7.0: rotation = -135
		
	rotate_z(deg_to_rad(rotation));
	
	if note_block._cutDirection == 8.0:
		$CutDirectionTriangle.visible = false

func _process(delta: float) -> void:
	var velocity: Vector3 = Vector3.BACK * speed
	
	position += velocity * delta

func _on_area_3d_area_entered(area: Area3D) -> void:
	if area.is_in_group("sabers"):
		queue_free()
