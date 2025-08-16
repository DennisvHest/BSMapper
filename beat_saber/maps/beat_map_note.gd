extends BeatMapObjectBase

class_name BeatMapNote

enum NoteBlockType { LEFT = 0, RIGHT = 1 }

enum CutDirection {
	UP = 0,
	DOWN = 1,
	LEFT = 2,
	RIGHT = 3,
	UP_LEFT = 4,
	UP_RIGHT = 5,
	DOWN_LEFT = 6,
	DOWN_RIGHT = 7,
	ANY = 8
}

var line_index: int
var line_layer: int
var type: NoteBlockType
var cut_direction: CutDirection

static func from_v2_object(original: Variant) -> BeatMapNote:
	var note = BeatMapNote.new()

	note.original_object = original
	note.beat = original._time
	note.line_index = original._lineIndex
	note.line_layer = original._lineLayer
	note.type = NoteBlockType.values()[int(original._type)]
	note.cut_direction = CutDirection.values()[int(original._cutDirection)]

	return note

func save_v2_object() -> void:
	super.save_v2_object()

	original_object._type = type
	original_object._cutDirection = cut_direction
	original_object._lineIndex = line_index
	original_object._lineLayer = line_layer
