extends BeatmapObject

class_name Wall

var duration_in_meters: float

func initialize(_initial_position: Vector3, _map_info: BeatMapDifficultyInfo, _wall: Variant):
	super.initialize(_initial_position, _map_info, _wall)
	
	var wall_width: float = _wall._width;
	var wall_height; 
	
	match _wall._type: 
		0.0: wall_height = 5 # Full-height walls
		1.0: wall_height = 3; position.y += 2 * 0.5 # Crouch walls (set position hard to middle layer)
		2.0: wall_height = _wall._height # Free walls (custom width/height)
	
	scale.y *= wall_height
	scale.x *= wall_width
	
	position.y += ((wall_height * 0.5) / 2) - 0.25
	position.x += ((wall_width * 0.5) / 2) - 0.25
	
	# Scale the length of the wall, based on the wall duration (in beats)
	var duration_in_seconds = _wall._duration / _map_info.bpm * 60
	duration_in_meters = duration_in_seconds * _map_info.njs
	
	scale.z *= duration_in_meters

func _process(delta: float) -> void:
	var jump_time = _get_jump_time()
	
	position.z = -_get_distance(jump_time)
	position.z -= (duration_in_meters / 2) - 0.25
	
	var visual_distance: float = _get_visual_distance(jump_time)
	$Visual.global_position.z = -visual_distance
	$Visual.global_position.z -= (duration_in_meters / 2) - 0.25
