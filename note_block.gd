extends Node3D

class_name NoteBlock

enum NoteBlockType { LEFT = 0, RIGHT = 1 }

static var njs: float = 16
var hjd: float
var bpm: float

var type: NoteBlockType
var block_rotation: float = 0
var jump_distance: float
var reaction_time: float
var half_jump_distance: float
var note_block: Variant
var note_time: float
var initial_position: Vector3

func initialize(_initial_position: Vector3, _note_block: Variant, _bpm: float, note_jump_start_beat_offset: float):
	initial_position = _initial_position
	position = initial_position
	note_block = _note_block
	bpm = _bpm
	
	var default_hjd: = 4.0
	
	while get_jump_distance(default_hjd) > 35.998:
		default_hjd /= 2
	
	hjd = max(default_hjd + note_jump_start_beat_offset, 0.25)
	
	jump_distance = get_jump_distance(hjd)
	reaction_time = jump_distance / 2 / njs
	half_jump_distance = reaction_time * njs
	
	note_time = note_block._time / bpm * 60
	
	set_note_block_color(note_block)
	set_cut_direction(note_block)

func get_jump_distance(hjd: float):
	var rt = 60.0 / bpm * hjd
	return njs * 2 * rt

func set_cut_direction(note_block: Variant):	
	match note_block._cutDirection:
		0.0: block_rotation = 180
		2.0: block_rotation = -90
		3.0: block_rotation = 90
		4.0: block_rotation = -135
		5.0: block_rotation = 135
		6.0: block_rotation = -45
		7.0: block_rotation = 45
	
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
	var jump_time = PlaybackManager.playback_position + reaction_time
	
	var distance: float	
	
	if note_time <= jump_time:
		var time_dist = note_time - PlaybackManager.playback_position
		distance = time_dist * njs
	else:
		var time_dist = (note_time - jump_time) / 0.2
		distance = half_jump_distance + (65 * time_dist)
		
	position.z = -distance
	
	if note_time > jump_time:
		position.y = 0
	else:
		var d_squared = pow(half_jump_distance, 2)
		var t_squared = pow(distance, 2)
		
		position.y = clamp(-(initial_position.y / d_squared) * t_squared + initial_position.y, -9999.0, 9999.0)
	
	var jump_progress = (jump_time - note_time) / reaction_time
	var rotation_animation_time = 0.2
	
	if jump_progress <= 0:
		rotation.z = 0
	elif jump_progress < rotation_animation_time:
		var rotation_progress = jump_progress / rotation_animation_time
		var angle_dist = ease(rotation_progress, 0.5)
		
		rotation.z = deg_to_rad(block_rotation * angle_dist)

func _on_area_3d_area_entered(area: Area3D) -> void:
	if area.is_in_group("sabers"):
		GameEvents.note_block_hit.emit(type)
		queue_free()
