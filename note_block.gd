extends Node3D

@export var speed: float = 400

func _process(delta: float) -> void:
	var velocity = Vector3.BACK * speed
	
	position += velocity * delta

func _on_area_3d_area_entered(area: Area3D) -> void:
	if area.is_in_group("sabers"):
		queue_free()
