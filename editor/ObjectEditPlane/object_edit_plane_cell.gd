extends Node3D

class_name ObjectEditPlaneCell

const DIRECTION_DRAG_THRESHOLD := 0.12
const PLANE_POSITION_TOLERANCE := 0.15
const LEFT_NOTE_PREVIEW_COLOR := Color(1, 0, 0, 0.6)
const RIGHT_NOTE_PREVIEW_COLOR := Color(0, 0, 1, 0.6)
const BOMB_PREVIEW_COLOR := Color(0.12, 0.12, 0.12, 0.75)
const PLACEABLE_NOTE_BLOCK := 0
const PLACEABLE_BOMB := 1
const NOTE_TYPE_LEFT := 0
const NOTE_TYPE_RIGHT := 1
const CUT_UP := 0
const CUT_DOWN := 1
const CUT_LEFT := 2
const CUT_RIGHT := 3
const CUT_UP_LEFT := 4
const CUT_UP_RIGHT := 5
const CUT_DOWN_LEFT := 6
const CUT_DOWN_RIGHT := 7
const CUT_ANY := 8

var line_index: int = 1
var line_layer: int = 2
var object_edit_plane: Node

var current_beatmap: BeatMap
var active_pointer_drags: Dictionary = {}

@onready var _note_preview_mesh: MeshInstance3D = $Preview/NotePreview
@onready var _bomb_preview_mesh: MeshInstance3D = $Preview/BombPreview

var _note_preview_material: StandardMaterial3D
var _bomb_preview_material: StandardMaterial3D

func set_object_edit_plane(p_object_edit_plane: Node) -> void:
    object_edit_plane = p_object_edit_plane

func initialize(p_line_index: int, p_line_layer: int) -> void:
    line_index = p_line_index
    line_layer = p_line_layer

func _ready() -> void:
    _note_preview_material = _note_preview_mesh.get_active_material(0).duplicate()
    _note_preview_mesh.set_surface_override_material(0, _note_preview_material)
    _bomb_preview_material = _bomb_preview_mesh.get_active_material(0).duplicate()
    _bomb_preview_mesh.set_surface_override_material(0, _bomb_preview_material)

    _on_beatmap_changed(BeatMapManager.CurrentBeatmap)
    BeatMapManager.CurrentBeatmapChanged.connect(_on_beatmap_changed)

# When the user interacts with the edit plane, we track the pointer events to determine the cut direction and create notes accordingly.
# A preview of the note is shown while dragging, and the final note is created on release.
func _on_edit_area_pointer_event(event: XRToolsPointerEvent) -> void:
    var pointer_id := event.pointer.get_instance_id()

    match event.event_type:
        XRToolsPointerEvent.Type.PRESSED:
            var local_press_position := to_local(event.position)
            var note_type := _get_note_type_for_pointer(event.pointer)
            var object_type: int = _get_selected_object_type()

            active_pointer_drags[pointer_id] = {
                "start_position": local_press_position,
                "cut_direction": CUT_DOWN,
                "beat": PlaybackManager.playback_beat,
                "note_type": note_type,
                "object_type": object_type,
            }
            _show_preview(object_type, CUT_DOWN, note_type)
        XRToolsPointerEvent.Type.MOVED:
            if not active_pointer_drags.has(pointer_id):
                return

            var drag_state: Dictionary = active_pointer_drags[pointer_id]
            var local_move_position := to_local(event.position)

            if drag_state.object_type == PLACEABLE_NOTE_BLOCK and _is_position_on_edit_plane(local_move_position):
                drag_state.cut_direction = _get_cut_direction_from_drag(
                    drag_state.start_position,
                    local_move_position
                )

            active_pointer_drags[pointer_id] = drag_state
            _show_preview(drag_state.object_type, drag_state.cut_direction, drag_state.note_type)
        XRToolsPointerEvent.Type.RELEASED:
            if not active_pointer_drags.has(pointer_id):
                return

            var drag_state: Dictionary = active_pointer_drags[pointer_id]
            active_pointer_drags.erase(pointer_id)
            _hide_preview()

            if current_beatmap == null:
                return

            if drag_state.object_type == PLACEABLE_BOMB:
                var new_bomb: BeatMapBomb = BeatMapBomb.new()
                new_bomb.Beat = drag_state.beat
                new_bomb.LineIndex = line_index
                new_bomb.LineLayer = line_layer
                current_beatmap.AddObject(new_bomb)
                return

            var new_note: BeatMapNote = BeatMapNote.new()
            new_note.Beat = drag_state.beat
            new_note.LineIndex = line_index
            new_note.LineLayer = line_layer
            new_note.Type = drag_state.note_type
            new_note.Cut = drag_state.cut_direction
            current_beatmap.AddObject(new_note)

func _on_beatmap_changed(beatmap: BeatMap) -> void:
    current_beatmap = beatmap

func _get_selected_object_type() -> int:
    if object_edit_plane == null:
        return PLACEABLE_NOTE_BLOCK

    return object_edit_plane.selected_object_type if "selected_object_type" in object_edit_plane else PLACEABLE_NOTE_BLOCK

func _get_note_type_for_pointer(pointer: Node) -> int:
    var current: Node = pointer

    while current != null:
        if current.name == "LeftHand":
            return NOTE_TYPE_LEFT
        if current.name == "RightHand":
            return NOTE_TYPE_RIGHT

        current = current.get_parent()

    return NOTE_TYPE_LEFT

func _get_cut_direction_from_drag(start_position: Vector3, current_position: Vector3) -> int:
    var drag_vector := Vector2(
        current_position.x - start_position.x,
        start_position.z - current_position.z
    )

    if drag_vector.length() < DIRECTION_DRAG_THRESHOLD:
        return CUT_DOWN

    var drag_angle := rad_to_deg(atan2(drag_vector.y, drag_vector.x))

    if drag_angle >= -22.5 and drag_angle < 22.5:
        return CUT_RIGHT
    if drag_angle >= 22.5 and drag_angle < 67.5:
        return CUT_UP_RIGHT
    if drag_angle >= 67.5 and drag_angle < 112.5:
        return CUT_UP
    if drag_angle >= 112.5 and drag_angle < 157.5:
        return CUT_UP_LEFT
    if drag_angle >= -67.5 and drag_angle < -22.5:
        return CUT_DOWN_RIGHT
    if drag_angle >= -112.5 and drag_angle < -67.5:
        return CUT_DOWN
    if drag_angle >= -157.5 and drag_angle < -112.5:
        return CUT_DOWN_LEFT

    return CUT_LEFT

func _is_position_on_edit_plane(local_position: Vector3) -> bool:
    return abs(local_position.y) <= PLANE_POSITION_TOLERANCE

func _show_preview(object_type: int, cut_direction: int, note_type: int) -> void:
    $Preview.show()

    if object_type == PLACEABLE_BOMB:
        _bomb_preview_material.albedo_color = BOMB_PREVIEW_COLOR
        $Preview.rotation.z = 0.0
        $Preview/NotePreview.visible = false
        $Preview/BombPreview.visible = true
        $Preview/CutDirectionTriangle.visible = false
        $Preview/AnyCutDirectionCircle.visible = false
        return

    _set_preview_color(note_type)
    $Preview.rotation.z = deg_to_rad(_get_cut_direction_rotation(cut_direction))
    $Preview/NotePreview.visible = true
    $Preview/BombPreview.visible = false
    $Preview/CutDirectionTriangle.visible = cut_direction != CUT_ANY
    $Preview/AnyCutDirectionCircle.visible = cut_direction == CUT_ANY

func _hide_preview() -> void:
    $Preview.hide()

func _set_preview_color(note_type: int) -> void:
    if note_type == NOTE_TYPE_RIGHT:
        _note_preview_material.albedo_color = RIGHT_NOTE_PREVIEW_COLOR
        return

    _note_preview_material.albedo_color = LEFT_NOTE_PREVIEW_COLOR

func _get_cut_direction_rotation(cut_direction: int) -> float:
    match cut_direction:
        CUT_UP:
            return 180.0
        CUT_LEFT:
            return 90.0
        CUT_RIGHT:
            return -90.0
        CUT_UP_LEFT:
            return 135.0
        CUT_UP_RIGHT:
            return -135.0
        CUT_DOWN_LEFT:
            return 45.0
        CUT_DOWN_RIGHT:
            return -45.0
        _:
            return 0.0