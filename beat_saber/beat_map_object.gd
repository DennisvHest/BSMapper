extends Node3D

class_name BeatmapObject

enum BeatmapObjectType { NOTE_BLOCK_LEFT = 0, NOTE_BLOCK_RIGHT = 1, BOMB = 3 }

## Time in seconds of the "snap in" animation. This is the animation before the note jump that moves the note block towards the half jump distance.
const SNAP_IN_ANIMATION_TIME := 0.2

## Distance in meters of the "snap in" animations.
const SNAP_IN_ANIMATION_DISTANCE := 65

var map_info: BeatMapDifficultyInfo

var beatmap_object: Variant
var initial_position: Vector3
var object_time: float

func initialize(_initial_position: Vector3, _map_info: BeatMapDifficultyInfo, _beatmap_object: Variant):
	initial_position = _initial_position
	position = initial_position
	map_info = _map_info
	beatmap_object = _beatmap_object
	
	object_time = beatmap_object._time / map_info.bpm * 60

func _get_jump_time() -> float:
	return PlaybackManager.playback_position + map_info.reaction_time

func _get_distance(jump_time: float) -> float:
	var time_dist = object_time - PlaybackManager.playback_position
	return time_dist * map_info.njs

func _get_visual_distance(jump_time: float) -> float:
	if object_time <= jump_time:
		# Object has already done it's jump animation, so move it towards the player at the note jump speed.
		return _get_distance(jump_time)
	else:
		# Object is not yet at the time to jump. Set the distance according to the snap in animation.
		var time_dist = (object_time - jump_time) / SNAP_IN_ANIMATION_TIME
		return map_info.half_jump_distance_meters + (SNAP_IN_ANIMATION_DISTANCE * time_dist)

func _get_visual_y(jump_time: float, distance: float) -> float:
	if object_time > jump_time:
		# Not jumping yet, so stay at the bottom
		return 0
	else:
		# Make object jump up
		var d_squared = pow(map_info.half_jump_distance_meters, 2)
		var t_squared = pow(distance, 2)
		
		return clamp(-(initial_position.y / d_squared) * t_squared + initial_position.y, -9999.0, 9999.0)
