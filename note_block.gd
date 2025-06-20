extends Node3D


func _on_area_3d_area_entered(area: Area3D) -> void:
	if area.is_in_group("sabers"):
		queue_free()
