
extends Node3D

class_name BeatmapObject

enum BeatmapObjectType { NOTE_BLOCK_LEFT = 0, NOTE_BLOCK_RIGHT = 1, BOMB = 3 }

## Time in seconds of the "snap in" animation. This is the animation before the note jump that moves the note block towards the half jump distance.
const SNAP_IN_ANIMATION_TIME := 0.2

## Distance in meters of the "snap in" animations.
const SNAP_IN_ANIMATION_DISTANCE := 65

var map_info: BeatMapDifficultyInfo

var beatmap_object: BeatMapObjectBase
var initial_position: Vector3
var object_time: float

var jump_animation_enabled: bool = true

var despawned: bool:
	get:
		return process_mode == ProcessMode.PROCESS_MODE_DISABLED

func _ready() -> void:
	_on_playback_mode_changed()
	PlaybackManager.mode_changed.connect(_on_playback_mode_changed)

func initialize(_initial_position: Vector3, _map_info: BeatMapDifficultyInfo, _beatmap_object: BeatMapObjectBase):
	initial_position = _initial_position
	position = initial_position
	map_info = _map_info
	beatmap_object = _beatmap_object
	
	object_time = beatmap_object.beat / map_info.bpm * 60

func _process(delta: float) -> void:
	var jump_time = _get_jump_time()
	var object_spawn_time = jump_time + map_info.reaction_time
	var object_despawn_time = object_spawn_time - map_info.reaction_time * 4
	
	if object_time <= object_spawn_time and object_time >= object_despawn_time:
		if not visible:
			show()
	else:
		if visible:
			hide()

func _on_playback_mode_changed() -> void:
	# Jump animation should be disabled in editing mode, so that the note blocks don't jump around when editing
	# Also respawn the object in edit mode, so hit blocks are visible again
	if PlaybackManager.mode == PlaybackManager.EditMode.EDITING:
		_spawn()
		set_jump_animation_enabled(false)
	else:
		set_jump_animation_enabled(true)

func set_jump_animation_enabled(enabled: bool) -> void:
	jump_animation_enabled = enabled

func _spawn() -> void:
	set_deferred('process_mode', ProcessMode.PROCESS_MODE_INHERIT)
	call_deferred('show')

## Despawn the object, hiding it instead of freeing it, so that it can be reused later.
## When going into editing mode, the objects are shown again.
## In edit mode, the objects are not despawned, so that they can be edited.
func _despawn() -> void:
	hide()
	set_deferred('process_mode', ProcessMode.PROCESS_MODE_DISABLED)

func delete_beatmap_object() -> void:
	if BeatMapManager.current_beatmap != null:
		BeatMapManager.current_beatmap.remove_object(beatmap_object)

	queue_free()

func _get_jump_time() -> float:
	return PlaybackManager.playback_position + map_info.reaction_time

func _get_distance(jump_time: float) -> float:
	var time_dist = object_time - PlaybackManager.playback_position
	return time_dist * map_info.njs

func _get_visual_distance(jump_time: float) -> float:
	if object_time <= jump_time or not jump_animation_enabled:
		# Object has already done it's jump animation, so move it towards the player at the note jump speed.
		return _get_distance(jump_time)
	else:
		# Object is not yet at the time to jump. Set the distance according to the snap in animation.
		var time_dist = (object_time - jump_time) / SNAP_IN_ANIMATION_TIME
		return map_info.half_jump_distance_meters + (SNAP_IN_ANIMATION_DISTANCE * time_dist)

func _get_visual_y(jump_time: float, distance: float) -> float:
	if not jump_animation_enabled:
		return _clamp_visual_y(0)

	if object_time > jump_time:
		# Not jumping yet, so stay at the bottom
		return 0
	else:
		# Make object jump up
		return _clamp_visual_y(distance)

func _clamp_visual_y(distance: float) -> float:
	var d_squared = pow(map_info.half_jump_distance_meters, 2)
	var t_squared = pow(distance, 2)
		
	return clamp(-(initial_position.y / d_squared) * t_squared + initial_position.y, -9999.0, 9999.0)
