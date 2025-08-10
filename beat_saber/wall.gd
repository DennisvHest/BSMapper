extends BeatmapObject

class_name Wall

var duration_in_meters: float

const FULL_WALL_TYPE: float = 0.0
const FULL_WALL_HEIGHT: float = 5.0

const CROUCH_WALL_TYPE: float = 1.0
const CROUCH_WALL_HEIGHT: float = 3.0

const FREE_WALL_TYPE: float = 2.0

func initialize_wall(_initial_position: Vector3, _map_info: BeatMapDifficultyInfo, _wall: BeatMapWall):
	super.initialize(_initial_position, _map_info, _wall)

	if _wall.type == BeatMapWall.WallType.CROUCH:
		position.y += 2 * NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE # Crouch walls (set position hard to middle layer)

	scale.y *= _wall.height
	scale.x *= _wall.width

	position.y += ((_wall.height * NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE) / 2) - NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE / 2
	position.x += ((_wall.width * NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE) / 2) - NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE / 2

	# Scale the length of the wall, based on the wall duration (in beats)
	var duration_in_seconds = _wall.duration / _map_info.bpm * 60
	duration_in_meters = duration_in_seconds * _map_info.njs
	
	scale.z *= duration_in_meters

func _process(delta: float) -> void:
	super._process(delta)
	
	if not visible:
		return
	
	var jump_time = _get_jump_time()
	
	position.z = -_get_distance(jump_time)
	position.z -= (duration_in_meters / 2) - 0.25
	
	var visual_distance: float = _get_visual_distance(jump_time)
	$Visual.global_position.z = -visual_distance
	$Visual.global_position.z -= (duration_in_meters / 2) - 0.25
