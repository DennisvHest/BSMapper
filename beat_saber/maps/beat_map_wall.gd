class_name BeatMapWall

enum WallType {
    FULL = 0,
    CROUCH = 1,
    FREE = 2
}

const FULL_WALL_HEIGHT: float = 5.0
const CROUCH_WALL_HEIGHT: float = 3.0

var original_object: Variant

var beat: float
var line_index: int
var line_layer: int
var type: int # 0 = full-height, 1 = crouch, 2 = free
var duration: float
var width: int
var height: int

static func from_v2_object(original: Variant) -> BeatMapWall:
    var wall = BeatMapWall.new()

    wall.original_object = original
    wall.beat = original._time
    wall.line_index = original._lineIndex
    wall.line_layer = original.has("_lineLayer") and original._lineLayer or 0
    wall.type = WallType[original._type]
    wall.duration = original._duration
    wall.width = original._width
    wall.height = original.has("_height") and original._height or 0

    return wall
