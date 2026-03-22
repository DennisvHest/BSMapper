extends Node3D

class_name ObjectEditPlaneCell

const DIRECTION_DRAG_THRESHOLD := 0.12

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

func _on_edit_area_pointer_event(event: XRToolsPointerEvent) -> void:
    var pointer_id := event.pointer.get_instance_id()

    match event.event_type:
        XRToolsPointerEvent.Type.PRESSED:
            active_pointer_drags[pointer_id] = {
                "start_position": to_local(event.position),
                "cut_direction": BeatMapNote.CutDirection.DOWN,
                "beat": PlaybackManager.playback_beat,
            }
        XRToolsPointerEvent.Type.MOVED:
            if not active_pointer_drags.has(pointer_id):
                return

            var drag_state: Dictionary = active_pointer_drags[pointer_id]
            drag_state.cut_direction = _get_cut_direction_from_drag(
                drag_state.start_position,
                to_local(event.position)
            )
            active_pointer_drags[pointer_id] = drag_state
        XRToolsPointerEvent.Type.RELEASED:
            if not active_pointer_drags.has(pointer_id):
                return

            var drag_state: Dictionary = active_pointer_drags[pointer_id]
            active_pointer_drags.erase(pointer_id)

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