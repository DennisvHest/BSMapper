extends Node3D

class_name BeatmapObject

## Time in seconds of the "snap in" animation. This is the animation before the note jump that moves the note block towards the half jump distance.
const SNAP_IN_ANIMATION_TIME := 0.2

## Distance in meters of the "snap in" animations.
const SNAP_IN_ANIMATION_DISTANCE := 65

var map_info: BeatMapDifficultyInfo

var note_block: Variant
var initial_position: Vector3
var note_time: float

func initialize(_initial_position: Vector3, _map_info: BeatMapDifficultyInfo, _note_block: Variant):
	initial_position = _initial_position
	position = initial_position
	map_info = _map_info
	note_block = _note_block
	
	note_time = note_block._time / map_info.bpm * 60

func _get_distance(jump_time: float) -> float:
	var time_dist = note_time - PlaybackManager.playback_position
	return time_dist * map_info.njs

func _get_visual_distance(jump_time: float) -> float:
	if note_time <= jump_time:
		# Note block has already done it's jump animation, so move it towards the player at the note jump speed.
		return _get_distance(jump_time)
	else:
		# Note block is not yet at the time to jump. Set the distance according to the snap in animation.
		var time_dist = (note_time - jump_time) / SNAP_IN_ANIMATION_TIME
		return map_info.half_jump_distance_meters + (SNAP_IN_ANIMATION_DISTANCE * time_dist)

func _get_visual_y(jump_time: float, distance: float) -> float:
	if note_time > jump_time:
		# Not jumping yet, so stay at the bottom
		return 0
	else:
		# Make note block jump up
		var d_squared = pow(map_info.half_jump_distance_meters, 2)
		var t_squared = pow(distance, 2)
		
		return clamp(-(initial_position.y / d_squared) * t_squared + initial_position.y, -9999.0, 9999.0)
