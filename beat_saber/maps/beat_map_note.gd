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

var original_object: Variant

var beat: float
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
    note.type = NoteBlockType[original._type]
    note.cut_direction = CutDirection[original._cutDirection]

    return note
