class_name BeatMap

signal object_added(object: BeatMapObjectBase)

var notes: Array[BeatMapNote] = []
var bombs: Array[BeatMapBomb] = []
var walls: Array[BeatMapWall] = []

func _init(map: Variant) -> void:
    assert(str(map._version).begins_with("2"), "Map version is not supported")

    for object in map._notes:
        if object._type == BeatMapBomb.BOMB_TYPE:
            bombs.append(BeatMapBomb.from_v2_object(object))
        else:
            notes.append(BeatMapNote.from_v2_object(object))

    for wall in map._obstacles:
        walls.append(BeatMapWall.from_v2_object(wall))

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
