extends Node3D

class_name NoteBlock

static var speed: float = 19

func initialize(initial_position: Vector3):
	position = initial_position

func _process(delta: float) -> void:
	var velocity: Vector3 = Vector3.BACK * speed
	
	position += velocity * delta

func _on_area_3d_area_entered(area: Area3D) -> void:
	if area.is_in_group("sabers"):
		queue_free()
