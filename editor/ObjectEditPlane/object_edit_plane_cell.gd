extends Node3D

class_name ObjectEditPlaneCell

var line_index: int = 1
var line_layer: int = 2

var current_beatmap: BeatMap

func initialize(p_line_index: int, p_line_layer: int) -> void:
    line_index = p_line_index
    line_layer = p_line_layer

func _ready() -> void:
    BeatMapManager.current_beatmap_changed.connect(_on_beatmap_changed)

func _on_edit_area_pointer_event(event: XRToolsPointerEvent) -> void:
    if not event.event_type == XRToolsPointerEvent.Type.PRESSED:
        return
    
    # Pointer has clicked on the edit area. Add a note block at this position.
    var beatmap_object: BeatMapNote = BeatMapNote.new()
    beatmap_object.beat = PlaybackManager.playback_beat
    beatmap_object.line_index = line_index
    beatmap_object.line_layer = line_layer
    beatmap_object.cut_direction = BeatMapNote.CutDirection.DOWN

    if current_beatmap != null:
        current_beatmap.notes.append(beatmap_object)
        BeatMapManager.change_beatmap(current_beatmap)

func _on_beatmap_changed(beatmap: BeatMap) -> void:
    current_beatmap = beatmap