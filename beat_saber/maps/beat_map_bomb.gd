class_name BeatMapBomb

var original_object: Variant

var beat: float
var line_index: int
var line_layer: int

static func from_v2_object(original: Variant) -> BeatMapBomb:
    var bomb = BeatMapBomb.new()

    bomb.original_object = original
    bomb.beat = original._time
    bomb.line_index = original._lineIndex
    bomb.line_layer = original._lineLayer
    
    return bomb
