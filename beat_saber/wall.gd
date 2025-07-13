extends BeatmapObject

class_name Wall

var duration_in_meters: float

func initialize(_initial_position: Vector3, _map_info: BeatMapDifficultyInfo, _wall: Variant):
	super.initialize(_initial_position, _map_info, _wall)
	
	match _wall._type:
		0.0: scale.y *= 3 # Full-height walls
		1.0: scale.y *= 2; position.z += 2 # Crouch walls
		3.0: assert(false, "TODO") # Free walls
	
	var duration_in_seconds = _wall._duration / _map_info.bpm * 60
	duration_in_meters = duration_in_seconds * _map_info.njs
	
	scale.z *= duration_in_meters

func _process(delta: float) -> void:
	var jump_time = _get_jump_time()
	
	position.z = -_get_distance(jump_time)
	position.z -= duration_in_meters / 2
	
	var visual_distance: float = _get_visual_distance(jump_time)
	$Visual.global_position.z = -visual_distance
	$Visual.global_position.z -= duration_in_meters / 2
