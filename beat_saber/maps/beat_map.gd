class_name BeatMap

signal object_added(object: BeatMapObjectBase)

var original_map: Variant

var notes: Array[BeatMapNote] = []
var bombs: Array[BeatMapBomb] = []
var walls: Array[BeatMapWall] = []

static func new_empty() -> BeatMap:
    var beat_map = BeatMap.new()

    beat_map.original_map = {
        _version = "2.0.0",
        _notes = [],
        _obstacles = [],
        _events = [],
        _customData = {}
    }

    return beat_map

static func from_file(map: Variant) -> BeatMap:
    assert(str(map._version).begins_with("2"), "Map version is not supported")

    var beat_map = BeatMap.new()

    beat_map.original_map = map

    for object in map._notes:
        if object._type == BeatMapBomb.BOMB_TYPE:
            beat_map.bombs.append(BeatMapBomb.from_v2_object(object))
        else:
            beat_map.notes.append(BeatMapNote.from_v2_object(object))

    for wall in map._obstacles:
        beat_map.walls.append(BeatMapWall.from_v2_object(wall))

    return beat_map

func add_object(object: BeatMapObjectBase) -> void:
    if object is BeatMapNote:
        notes.append(object)
    elif object is BeatMapBomb:
        bombs.append(object)
    elif object is BeatMapWall:
        walls.append(object)
    else:
        assert(false, "Unknown beatmap object type")

    object_added.emit(object)

func save_changes() -> void:
    original_map._notes.clear()
    original_map._obstacles.clear()

    var all_notes := []

    for note in notes:
        note.save_v2_object()
        all_notes.append(note.original_object)
    for bomb in bombs:
        bomb.save_v2_object()
        all_notes.append(bomb.original_object)

    all_notes.sort_custom(func(a, b): return a._time < b._time)

    for obj in all_notes:
        original_map._notes.append(obj)

    var all_walls := []

    for wall in walls:
        wall.save_v2_object()
        all_walls.append(wall.original_object)

    all_walls.sort_custom(func(a, b): return a._time < b._time)

    for obj in all_walls:
        original_map._obstacles.append(obj)
