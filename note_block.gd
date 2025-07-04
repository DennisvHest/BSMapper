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

func initialize(initial_position: Vector3, _note_block: Variant, _bpm: float, note_jump_start_beat_offset: float):
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
	
	rotate_z(deg_to_rad(block_rotation))

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

func _on_area_3d_area_entered(area: Area3D) -> void:
	if area.is_in_group("sabers"):
		GameEvents.note_block_hit.emit(type)
		queue_free()
