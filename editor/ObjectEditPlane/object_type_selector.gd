extends Node3D

class_name ObjectTypeSelector

const PLACEABLE_NOTE_BLOCK := 0
const PLACEABLE_BOMB := 1

var object_edit_plane: Node
var _selector_ui: Node

func _ready() -> void:
	object_edit_plane = get_parent()
	assert(object_edit_plane != null, "ObjectTypeSelector must have a parent")

	_selector_ui = $ViewportPanel.get_scene_instance()
	assert(_selector_ui != null, "ObjectTypeSelector viewport scene failed to initialize")

	_selector_ui.placeable_selected.connect(_on_placeable_selected)
	object_edit_plane.selected_object_type_changed.connect(_on_selected_object_type_changed)
	_update_selection_visuals(object_edit_plane.selected_object_type)

func _on_placeable_selected(selected_object_type: int) -> void:
	object_edit_plane.set_selected_object_type(selected_object_type)

func _on_selected_object_type_changed(selected_object_type: int) -> void:
	_update_selection_visuals(selected_object_type)

func _update_selection_visuals(selected_object_type: int) -> void:
	if _selector_ui != null:
		_selector_ui.set_selected_object_type(selected_object_type)