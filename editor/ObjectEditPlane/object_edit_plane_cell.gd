extends Node3D

class_name ObjectEditPlaneCell

var line_index: int = 1
var line_layer: int = 2

var current_beatmap: BeatMap

func initialize(p_line_index: int, p_line_layer: int) -> void:
    line_index = p_line_index
    line_layer = p_line_layer

func _ready() -> void:
    _on_beatmap_changed(BeatMapManager.current_beatmap)
    BeatMapManager.current_beatmap_changed.connect(_on_beatmap_changed)

func _on_edit_area_pointer_event(event: XRToolsPointerEvent) -> void:
    if not event.event_type == XRToolsPointerEvent.Type.PRESSED:
        return
    
    # Pointer has clicked on the edit area. Add a note block at this position.
    var new_note: BeatMapNote = BeatMapNote.new()
    new_note.beat = PlaybackManager.playback_beat
    new_note.line_index = line_index
    new_note.line_layer = line_layer
    new_note.cut_direction = BeatMapNote.CutDirection.DOWN

    if current_beatmap != null:
        current_beatmap.add_object(new_note)

func _on_beatmap_changed(beatmap: BeatMap) -> void:
    current_beatmap = beatmap