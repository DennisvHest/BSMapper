extends Node3D

class_name ObjectEditPlane

signal selected_object_type_changed(selected_object_type)

enum PlaceableObjectType { NOTE_BLOCK, BOMB }

const OBJECT_SELECTOR_MARGIN := 0.95
const OBJECT_SELECTOR_Z_OFFSET := 0.12

@export var object_edit_plane_cell_scene: PackedScene

var selected_object_type: PlaceableObjectType = PlaceableObjectType.NOTE_BLOCK

func _ready() -> void:
	_position_object_type_selector()
	_on_playback_mode_changed()
	_spawn_grid_cells()
	PlaybackManager.mode_changed.connect(_on_playback_mode_changed)

func set_selected_object_type(new_selected_object_type: PlaceableObjectType) -> void:
	if selected_object_type == new_selected_object_type:
		return

	selected_object_type = new_selected_object_type
	selected_object_type_changed.emit(selected_object_type)

func _position_object_type_selector() -> void:
	$ObjectTypeSelector.position = Vector3(
		-NoteBlockLane.LANE_WIDTH / 2.0 - OBJECT_SELECTOR_MARGIN,
		GlobalSettings.player_height / 3.0 + NoteBlockLane.LANE_HEIGHT / 2.0,
		OBJECT_SELECTOR_Z_OFFSET
	)

func _spawn_grid_cells() -> void:
	for line_index in NoteBlockLane.GRID_WIDTH:
		for line_layer in NoteBlockLane.GRID_HEIGHT:
			var cell: ObjectEditPlaneCell = object_edit_plane_cell_scene.instantiate()

			cell.set_object_edit_plane(self)
			cell.initialize(line_index, line_layer)

			# Calculate position of cell based on grid coordinates
			cell.position += Vector3.RIGHT * NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE * line_index
			cell.position += Vector3.UP * NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE * line_layer

			# Center grid horizontally
			cell.position += Vector3.LEFT * NoteBlockLane.LANE_WIDTH / 2
			cell.position += Vector3.RIGHT * NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE / 2

			# Center grid vertically
			cell.position += Vector3.UP * GlobalSettings.player_height * 1 / 3
			cell.position += Vector3.UP * NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE / 2

			add_child(cell)

func _on_playback_mode_changed() -> void:
	if PlaybackManager.mode == PlaybackManager.EditMode.EDITING:
		show()
	else:
		hide()
