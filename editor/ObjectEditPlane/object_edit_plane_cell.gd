extends Node3D

class_name ObjectEditPlaneCell

const DIRECTION_DRAG_THRESHOLD := 0.12
const PLANE_POSITION_TOLERANCE := 0.15

var line_index: int = 1
var line_layer: int = 2

var current_beatmap: BeatMap
var active_pointer_drags: Dictionary = {}

func initialize(p_line_index: int, p_line_layer: int) -> void:
    line_index = p_line_index
    line_layer = p_line_layer

func _ready() -> void:
    _on_beatmap_changed(BeatMapManager.current_beatmap)
    BeatMapManager.current_beatmap_changed.connect(_on_beatmap_changed)

# When the user interacts with the edit plane, we track the pointer events to determine the cut direction and create notes accordingly.
# A preview of the note is shown while dragging, and the final note is created on release.
func _on_edit_area_pointer_event(event: XRToolsPointerEvent) -> void:
    var pointer_id := event.pointer.get_instance_id()

    match event.event_type:
        XRToolsPointerEvent.Type.PRESSED:
            var local_press_position := to_local(event.position)

            active_pointer_drags[pointer_id] = {
                "start_position": local_press_position,
                "cut_direction": BeatMapNote.CutDirection.DOWN,
                "beat": PlaybackManager.playback_beat,
            }
            _show_preview(BeatMapNote.CutDirection.DOWN)
        XRToolsPointerEvent.Type.MOVED:
            if not active_pointer_drags.has(pointer_id):
                return

            var drag_state: Dictionary = active_pointer_drags[pointer_id]
            var local_move_position := to_local(event.position)

            if _is_position_on_edit_plane(local_move_position):
                drag_state.cut_direction = _get_cut_direction_from_drag(
                    drag_state.start_position,
                    local_move_position
                )

            active_pointer_drags[pointer_id] = drag_state
            _show_preview(drag_state.cut_direction)
        XRToolsPointerEvent.Type.RELEASED:
            if not active_pointer_drags.has(pointer_id):
                return

            var drag_state: Dictionary = active_pointer_drags[pointer_id]
            active_pointer_drags.erase(pointer_id)
            _hide_preview()

            if current_beatmap == null:
                return

            var new_note: BeatMapNote = BeatMapNote.new()
            new_note.beat = drag_state.beat
            new_note.line_index = line_index
            new_note.line_layer = line_layer
            new_note.cut_direction = drag_state.cut_direction
            current_beatmap.add_object(new_note)

func _on_beatmap_changed(beatmap: BeatMap) -> void:
    current_beatmap = beatmap

func _get_cut_direction_from_drag(start_position: Vector3, current_position: Vector3) -> BeatMapNote.CutDirection:
    var drag_vector := Vector2(
        current_position.x - start_position.x,
        start_position.z - current_position.z
    )

    if drag_vector.length() < DIRECTION_DRAG_THRESHOLD:
        return BeatMapNote.CutDirection.DOWN

    var drag_angle := rad_to_deg(atan2(drag_vector.y, drag_vector.x))

    if drag_angle >= -22.5 and drag_angle < 22.5:
        return BeatMapNote.CutDirection.RIGHT
    if drag_angle >= 22.5 and drag_angle < 67.5:
        return BeatMapNote.CutDirection.UP_RIGHT
    if drag_angle >= 67.5 and drag_angle < 112.5:
        return BeatMapNote.CutDirection.UP
    if drag_angle >= 112.5 and drag_angle < 157.5:
        return BeatMapNote.CutDirection.UP_LEFT
    if drag_angle >= -67.5 and drag_angle < -22.5:
        return BeatMapNote.CutDirection.DOWN_RIGHT
    if drag_angle >= -112.5 and drag_angle < -67.5:
        return BeatMapNote.CutDirection.DOWN
    if drag_angle >= -157.5 and drag_angle < -112.5:
        return BeatMapNote.CutDirection.DOWN_LEFT

    return BeatMapNote.CutDirection.LEFT

func _is_position_on_edit_plane(local_position: Vector3) -> bool:
    return abs(local_position.y) <= PLANE_POSITION_TOLERANCE

func _show_preview(cut_direction: BeatMapNote.CutDirection) -> void:
    $Preview.show()
    $Preview.rotation.z = deg_to_rad(_get_cut_direction_rotation(cut_direction))
    $Preview/CutDirectionTriangle.visible = cut_direction != BeatMapNote.CutDirection.ANY
    $Preview/AnyCutDirectionCircle.visible = cut_direction == BeatMapNote.CutDirection.ANY

func _hide_preview() -> void:
    $Preview.hide()

func _get_cut_direction_rotation(cut_direction: BeatMapNote.CutDirection) -> float:
    match cut_direction:
        BeatMapNote.CutDirection.UP:
            return 180.0
        BeatMapNote.CutDirection.LEFT:
            return 90.0
        BeatMapNote.CutDirection.RIGHT:
            return -90.0
        BeatMapNote.CutDirection.UP_LEFT:
            return 135.0
        BeatMapNote.CutDirection.UP_RIGHT:
            return -135.0
        BeatMapNote.CutDirection.DOWN_LEFT:
            return 45.0
        BeatMapNote.CutDirection.DOWN_RIGHT:
            return -45.0
        _:
            return 0.0