extends Node3D

class_name NoteBlock

enum NoteBlockType { LEFT = 0, RIGHT = 1 }

static var speed: float = 16

var velocity: Vector3 = Vector3.BACK * speed

var hit_time: float
var initial_position: Vector3
var jump_duration: float
var half_jump_duration: float
var spawn_distance: Vector3
var type: NoteBlockType

func initialize(_initial_position: Vector3, note_block: Variant, bpm: float, note_jump_start_beat_offset: float):
	hit_time = note_block._time * 60 / bpm
	initial_position = _initial_position
	position = initial_position
	
	# Not sure if this is the right calculation
	jump_duration = max(0.65, (60 / bpm) * (note_jump_start_beat_offset + 4))
	half_jump_duration = jump_duration / 2
	var jump_distance = speed * jump_duration
	spawn_distance = Vector3.FORWARD * jump_distance / 2
	
	var material: StandardMaterial3D = $Visual/MeshInstance3D.get_active_material(0)
	
	if note_block._type == NoteBlockType.LEFT:
		type = NoteBlockType.LEFT
		material.albedo_color = Color.RED
	elif note_block._type == NoteBlockType.RIGHT:
		type = NoteBlockType.RIGHT
		material.albedo_color = Color.BLUE
	
	set_cut_direction(note_block)

func set_cut_direction(note_block: Variant):
	var block_rotation = 0;
	
	match note_block._cutDirection:
		1.0: block_rotation = 180
		2.0: block_rotation = -90
		3.0: block_rotation = 90
		4.0: block_rotation = 45
		5.0: block_rotation = -45
		6.0: block_rotation = 135
		7.0: block_rotation = -135
		
	rotate_z(deg_to_rad(block_rotation));
	
	if note_block._cutDirection == 8.0:
		$Visual/CutDirectionTriangle.visible = false

func _process(delta: float) -> void:
	position = initial_position + velocity * PlaybackManager.playback_position
		
	if position.z < spawn_distance.z * 2:
		visible = false
	else:
		visible = true
	
	var move_time = 0.3
	var move_speed = 200
	var warmup_position = -move_time * move_speed
	
	var timeOffset = hit_time - PlaybackManager.playback_position - half_jump_duration - move_time;
	
	if timeOffset <= -move_time:
		# Visual at same position as parent (NoteBlock)
		$Visual.position = Vector3.ZERO
	else:
		# Currently in jump animation
		$Visual.position.z = warmup_position + move_speed * -timeOffset;

func _on_area_3d_area_entered(area: Area3D) -> void:
	if area.is_in_group("sabers"):
		GameEvents.note_block_hit.emit(type)
		queue_free()
