class_name BeatMapDifficultyInfo

## The default half jump distance is 4 beats away from the player.
const DEFAULT_HALF_JUMP_DISTANCE := 4.0

## Maximum full jump distance in meters. This is an community-accepted approximation of the internal value used in Beat Saber.
const MAX_JUMP_DISTANCE_METERS := 35.998

## Minimum half jump distance to avoid reaction time being too short.
const MIN_HALF_JUMP_DISTANCE := 0.25

## Note jump speed: speed of note blocks in m/s
var njs: float = 16.0

## The offset of the DEFAULT_HALF_JUMP_DISTANCE (in beats).
## As an example, this is used by mappers to align the note jumps to the rythm of the song.
var note_jump_start_beat_offset: float = -0.15

## Beats per minute of the song
var bpm := 175

## The half jump distance (in beats) is where the notes "jump" up after which they're on their target position/speed going towards the player.
var half_jump_distance: float:
	get:
		return half_jump_distance
	set(value):
		half_jump_distance = value
		jump_distance_meters = _get_jump_distance_meters(half_jump_distance, bpm, njs)

## Total jump distance in meters
var jump_distance_meters: float:
	get:
		return jump_distance_meters
	set(value):
		jump_distance_meters = value
		half_jump_distance_meters = jump_distance_meters / 2

## Half jump distance in meters
var half_jump_distance_meters: float:
	get:
		return half_jump_distance_meters
	set(value):
		half_jump_distance_meters = value
		reaction_time = half_jump_distance_meters / njs

## Time from when the note jumps up to when the player is supposed to hit it (in seconds)
var reaction_time: float

func _init() -> void:
	half_jump_distance = _get_half_jump_distance(bpm, njs, note_jump_start_beat_offset)
	jump_distance_meters = _get_jump_distance_meters(half_jump_distance, bpm, njs)
	reaction_time = jump_distance_meters / 2 / njs

## Converts the given half jump distance (in beats) to the full jump distance in meters
func _get_jump_distance_meters(half_jump_distance: float, bpm: float, njs: float) -> float:
	var half_jump_distance_seconds = 60.0 / bpm * half_jump_distance
	return njs * 2 * half_jump_distance_seconds

## Calculates the half jump distance (in beats) of this map and applies the note jump start beat offset
## This is mimicking Beat Saber's clamping behavior — the game prevents the jump distance (i.e. how far away notes spawn) from going over a certain threshold, typically around 36 meters.
func _get_half_jump_distance(bpm: float, njs: float, note_jump_start_beat_offset: float) -> float:
	var half_jump_distance: float = DEFAULT_HALF_JUMP_DISTANCE
	
	while _get_jump_distance_meters(half_jump_distance, bpm, njs) > MAX_JUMP_DISTANCE_METERS:
		half_jump_distance /= 2
	
	return max(half_jump_distance + note_jump_start_beat_offset, MIN_HALF_JUMP_DISTANCE)
