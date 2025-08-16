extends BeatMapObjectBase

class_name BeatMapWall

enum WallType {
	FULL = 0,
	CROUCH = 1,
	FREE = 2
}

const FULL_WALL_HEIGHT: float = 5.0
const CROUCH_WALL_HEIGHT: float = 3.0

var line_index: int
var line_layer: int
var type: WallType
var duration: float
var width: int
var height: int

static func from_v2_object(original: Variant) -> BeatMapWall:
	var wall = BeatMapWall.new()

	wall.original_object = original
	wall.beat = original._time
	wall.line_index = original._lineIndex
	wall.line_layer = original.has("_lineLayer") and original._lineLayer or 0
	wall.type = WallType.values()[int(original._type)]
	wall.duration = original._duration
	wall.width = original._width

	# Set height based on wall type
	match wall.type:
		WallType.FULL:
			wall.height = FULL_WALL_HEIGHT
		WallType.CROUCH:
			wall.height = CROUCH_WALL_HEIGHT
		_:
			wall.height = original.has("_height") and original._height or 0

	return wall

func save_v2_object() -> void:
	super.save_v2_object()

	original_object._type = type
	original_object._duration = duration
	original_object._lineIndex = line_index
	original_object._width = width
