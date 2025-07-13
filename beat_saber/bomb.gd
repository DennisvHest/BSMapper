extends BeatmapObject

class_name Bomb

func initialize(_initial_position: Vector3, _map_info: BeatMapDifficultyInfo, _note_block: Variant):
	super.initialize(_initial_position, _map_info, _note_block)

func _process(delta: float) -> void:
	var jump_time = _get_jump_time()
	
	position.z = -_get_distance(jump_time)
	
	var visual_distance: float = _get_visual_distance(jump_time)
	$Visual.global_position.z = -visual_distance
	$Visual.global_position.y = _get_visual_y(jump_time, visual_distance)

func _on_area_3d_area_entered(area: Area3D) -> void:
	if area.is_in_group(Groups.sabers):
		var saber: Saber = area.get_parent()
		
		assert(saber is Saber, "Expected parent to be Saber")
		
		GameEvents.bomb_hit.emit(saber.type)
		queue_free()
