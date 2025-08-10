class_name beat_map

var notes: Array[BeatMapNote] = []
var bombs: Array[BeatMapBomb] = []
var walls: Array[BeatMapWall] = []

func _init(map: Variant) -> void:
    assert(str(map._version).begins_with("2"), "Map version is not supported")

    for note in map.notes:
        notes.append(BeatMapNote.from_v2_object(note))
    for bomb in map.bombs:
        bombs.append(BeatMapBomb.from_v2_object(bomb))
    for wall in map.walls:
        walls.append(BeatMapWall.from_v2_object(wall))
