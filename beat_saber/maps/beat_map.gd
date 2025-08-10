class_name BeatMap

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
