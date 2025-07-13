extends BeatmapObject

class_name NoteBlock

enum NoteBlockType { LEFT = 0, RIGHT = 1 }

## Note block rotates to correct cut direction during jump animation. Sets time (in seconds) of animation.
const ROTATION_ANIMATION_TIME := 0.2

var type: NoteBlockType
var block_rotation: float = 0

func initialize(_initial_position: Vector3, _map_info: BeatMapDifficultyInfo, _note_block: Variant):
	super.initialize(_initial_position, _map_info, _note_block)
	
	set_note_block_color(note_block)
	set_cut_direction(note_block)

func set_cut_direction(note_block: Variant):	
	match note_block._cutDirection:
		0.0: block_rotation = 180
		2.0: block_rotation = -90
		3.0: block_rotation = 90
		4.0: block_rotation = -135
		5.0: block_rotation = 135
		6.0: block_rotation = -45
		7.0: block_rotation = 45
	
	rotation.z = deg_to_rad(block_rotation)
	
	if note_block._cutDirection == 8.0:
		$Visual/CutDirectionTriangle.visible = false
		$Visual/AnyCutDirectionCircle.visible = true
	else:
		$Visual/CutDirectionTriangle.visible = true
		$Visual/AnyCutDirectionCircle.visible = false

func set_note_block_color(note_block: Variant):
	var material: StandardMaterial3D = $Visual/MeshInstance3D.get_active_material(0)
	
	if note_block._type == NoteBlockType.LEFT:
		type = NoteBlockType.LEFT
		material.albedo_color = Color.RED
	elif note_block._type == NoteBlockType.RIGHT:
		type = NoteBlockType.RIGHT
		material.albedo_color = Color.BLUE

func _process(delta: float) -> void:
	var jump_time = PlaybackManager.playback_position + map_info.reaction_time
	
	position.z = -_get_distance(jump_time)
	
	var visual_distance: float = _get_visual_distance(jump_time)
	$Visual.global_position.z = -visual_distance
	$Visual.global_position.y = _get_visual_y(jump_time, visual_distance)
	$Visual.global_rotation.z = _get_note_visual_rotation(jump_time)

func _get_note_visual_rotation(jump_time: float) -> float:
	var jump_progress = (jump_time - note_time) / map_info.reaction_time
	var ROTATION_ANIMATION_TIME = 0.2
	
	if jump_progress <= 0:
		return 0 # Before rotation animation, so no rotation
	
	if jump_progress < ROTATION_ANIMATION_TIME:
		# In rotation animation
		var rotation_progress = jump_progress / ROTATION_ANIMATION_TIME
		var angle_dist = ease(rotation_progress, 0.5)
		
		return deg_to_rad(block_rotation * angle_dist)
	
	# After rotation animation, so rotated to final rotation
	return deg_to_rad(block_rotation)

func _on_area_3d_area_entered(area: Area3D) -> void:
	if area.is_in_group("sabers"):
		GameEvents.note_block_hit.emit(type)
		queue_free()
