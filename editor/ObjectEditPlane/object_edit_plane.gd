extends Node3D

@export var object_edit_plane_cell_scene: PackedScene

func _ready() -> void:
    _spawn_grid_cells()
    PlaybackManager.mode_changed.connect(_on_playback_mode_changed)

func _spawn_grid_cells():
    for x in NoteBlockLane.GRID_WIDTH:
        for y in NoteBlockLane.GRID_HEIGHT:
            var cell = object_edit_plane_cell_scene.instantiate()

            # Calculate position of cell based on grid coordinates
            cell.position += Vector3.RIGHT * NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE * x
            cell.position += Vector3.UP * NoteBlockLane.BEATMAP_OBJECT_LINE_SIZE * y

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