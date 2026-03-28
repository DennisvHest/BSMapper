extends BeatmapObject

class_name NoteBlock

## Note block rotates to correct cut direction during jump animation. Sets time (in seconds) of animation.
const ROTATION_ANIMATION_TIME := 0.2

@onready var _highlight_outline: MeshInstance3D = $Visual/HighlightOutline

var block_rotation: float = 0
var _hovering_pointers: Dictionary = {}

func _ready() -> void:
	super._ready()
	_set_highlight_visible(false)
	visibility_changed.connect(_on_visibility_changed)

func initialize_note(_initial_position: Vector3, _map_info: BeatMapDifficultyInfo, _note_block: BeatMapNote):
	super.initialize(_initial_position, _map_info, _note_block)
	
	set_note_block_color(_note_block)
	set_cut_direction(_note_block)

func set_cut_direction(note_block: BeatMapNote):
	match note_block.cut_direction:
		BeatMapNote.CutDirection.UP: block_rotation = 180
		BeatMapNote.CutDirection.LEFT: block_rotation = -90
		BeatMapNote.CutDirection.RIGHT: block_rotation = 90
		BeatMapNote.CutDirection.UP_LEFT: block_rotation = -135
		BeatMapNote.CutDirection.UP_RIGHT: block_rotation = 135
		BeatMapNote.CutDirection.DOWN_LEFT: block_rotation = -45
		BeatMapNote.CutDirection.DOWN_RIGHT: block_rotation = 45

	rotation.z = deg_to_rad(block_rotation)

	if note_block.cut_direction == BeatMapNote.CutDirection.ANY:
		$Visual/CutDirectionTriangle.visible = false
		$Visual/AnyCutDirectionCircle.visible = true
	else:
		$Visual/CutDirectionTriangle.visible = true
		$Visual/AnyCutDirectionCircle.visible = false

func set_note_block_color(note_block: BeatMapNote):
	var material: StandardMaterial3D = $Visual/MeshInstance3D.get_active_material(0)

	if note_block.type == BeatMapNote.NoteBlockType.LEFT:
		material.albedo_color = Color.RED
	elif note_block.type == BeatMapNote.NoteBlockType.RIGHT:
		material.albedo_color = Color.BLUE

func _process(delta: float) -> void:
	super._process(delta)
	
	if not visible or despawned:
		return
	
	var jump_time = _get_jump_time()
	
	position.z = -_get_distance(jump_time)
	
	var visual_distance: float = _get_visual_distance(jump_time)
	$Visual.global_position.z = -visual_distance
	$Visual.global_position.y = _get_visual_y(jump_time, visual_distance)
	$Visual.global_rotation.z = _get_note_visual_rotation(jump_time)

func _get_note_visual_rotation(jump_time: float) -> float:
	if not jump_animation_enabled:
		return deg_to_rad(block_rotation)

	var jump_progress = (jump_time - object_time) / map_info.reaction_time
	
	if jump_progress <= 0:
		return 0 # Before rotation animation, so no rotation
	
	if jump_progress < ROTATION_ANIMATION_TIME:
		# In rotation animation
		var rotation_progress = jump_progress / ROTATION_ANIMATION_TIME
		var angle_dist = ease(rotation_progress, 0.5)
		
		return deg_to_rad(block_rotation * angle_dist)
	
	# After rotation animation, so rotated to final rotation
	return deg_to_rad(block_rotation)

func _on_visibility_changed() -> void:
	if not visible:
		_hovering_pointers.clear()
		_set_highlight_visible(false)

	call_deferred("_change_collision_on_visibility_changed");

func _change_collision_on_visibility_changed() -> void:
	$Area3D/CollisionShape3D.disabled = not visible

# Highlight note block when pointer is hovering over it
func _on_area_3d_pointer_event(event: XRToolsPointerEvent) -> void:
	var pointer_id := event.pointer.get_instance_id()

	match event.event_type:
		XRToolsPointerEvent.Type.ENTERED:
			_hovering_pointers[pointer_id] = true
		XRToolsPointerEvent.Type.EXITED:
			_hovering_pointers.erase(pointer_id)
		_:
			return

	_set_highlight_visible(not _hovering_pointers.is_empty())

func _set_highlight_visible(visible_state: bool) -> void:
	_highlight_outline.visible = visible_state

func _on_area_3d_area_entered(area: Area3D) -> void:
	if area.is_in_group(Groups.sabers):
		if PlaybackManager.mode == PlaybackManager.EditMode.EDITING:
			return # Note blocks should not be hit in edit mode

		var saber: Saber = area.get_parent()
		
		assert(saber is Saber, "Expected parent to be Saber")
		
		GameEvents.note_block_hit.emit(saber.type)
		_despawn()
