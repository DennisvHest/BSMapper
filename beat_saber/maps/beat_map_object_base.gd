class_name BeatMapObjectBase

var original_object: Variant

var beat: float

func save_v2_object() -> void:
    if not original_object:
        original_object = {}

    original_object._time = beat