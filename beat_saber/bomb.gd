extends BeatmapObject

class_name Bomb

@onready var _highlight_outline: MeshInstance3D = $Visual/HighlightOutline

var _hovering_pointers: Dictionary = {}

func _ready() -> void:
	super._ready()
	_set_highlight_visible(false)
	visibility_changed.connect(_on_visibility_changed)

func initialize_bomb(_initial_position: Vector3, _map_info: BeatMapDifficultyInfo, _bomb: BeatMapBomb):
	super.initialize(_initial_position, _map_info, _bomb)

func _process(delta: float) -> void:
	super._process(delta)
	
	if not visible:
		return
	
	var jump_time = _get_jump_time()
	
	position.z = -_get_distance(jump_time)
	
	var visual_distance: float = _get_visual_distance(jump_time)
	$Visual.global_position.z = -visual_distance
	$Visual.global_position.y = _get_visual_y(jump_time, visual_distance)

func _on_visibility_changed() -> void:
	if not visible:
		_hovering_pointers.clear()
		_set_highlight_visible(false)

	call_deferred("_change_collision_on_visibility_changed");

func _change_collision_on_visibility_changed() -> void:
	$Area3D/CollisionShape3D.disabled = not visible

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
			return # Bombs should not be hit in edit mode
		
		var saber: Saber = area.get_parent()
		
		assert(saber is Saber, "Expected parent to be Saber")
		
		GameEvents.bomb_hit.emit(saber.type)
		_despawn()
