class_name 	BeatMapDifficultyInfo

enum Difficulty {
	EASY,
	NORMAL,
	HARD,
	EXPERT,
	EXPERT_PLUS
}

## The default half jump distance is 4 beats away from the player.
const DEFAULT_HALF_JUMP_DISTANCE := 4.0

## Maximum full jump distance in meters. This is an community-accepted approximation of the internal value used in Beat Saber.
const MAX_JUMP_DISTANCE_METERS := 35.998

## Minimum half jump distance to avoid reaction time being too short.
const MIN_HALF_JUMP_DISTANCE := 0.25

var difficulty: Difficulty
func to_v2_object() -> Dictionary:
	return {
		"_difficulty": get_difficulty_name(difficulty),
		"_beatmapFilename": beat_map_file_name,
		"_noteJumpMovementSpeed": njs,
		"_noteJumpStartBeatOffset": note_jump_start_beat_offset
	}

var beat_map_file_name: String

## Note jump speed: speed of note blocks in m/s
var njs: float

## The offset of the DEFAULT_HALF_JUMP_DISTANCE (in beats).
## As an example, this is used by mappers to align the note jumps to the rythm of the song.
var note_jump_start_beat_offset: float

## Beats per minute of the song
var bpm: float

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

## Duration of a beat (in seconds)
var beat_duration: float:
	get:
		if bpm == 0:
			return 0.0

		return 60.0 / bpm

## Time from when the note jumps up to when the player is supposed to hit it (in seconds)
var reaction_time: float

func initialize() -> void:
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

static func new_difficulty(difficulty: Difficulty, mode: BeatMapDifficultySet.BeatmapMode, njs: float, note_jump_start_beat_offset: float, bpm: float) -> BeatMapDifficultyInfo:
	var new_difficulty_info = BeatMapDifficultyInfo.new()

	new_difficulty_info.difficulty = difficulty
	new_difficulty_info.beat_map_file_name = get_file_name(difficulty, mode)
	new_difficulty_info.njs = njs
	new_difficulty_info.note_jump_start_beat_offset = note_jump_start_beat_offset
	new_difficulty_info.bpm = bpm

	new_difficulty_info.initialize()

	return new_difficulty_info

static func from_v2_object(original: Variant, _bpm: float) -> BeatMapDifficultyInfo:
	var info = BeatMapDifficultyInfo.new()

	info.difficulty = get_difficulty(original._difficulty)
	info.beat_map_file_name = original._beatmapFilename
	info.njs = original._noteJumpMovementSpeed
	info.note_jump_start_beat_offset = original._noteJumpStartBeatOffset
	info.bpm = _bpm

	info.initialize()

	return info

static func get_difficulty(difficulty: String) -> Difficulty:
	match difficulty:
		"Easy":
			return Difficulty.EASY
		"Normal":
			return Difficulty.NORMAL
		"Hard":
			return Difficulty.HARD
		"Expert":
			return Difficulty.EXPERT
		"ExpertPlus":
			return Difficulty.EXPERT_PLUS
		_:
			return Difficulty.EASY # TODO: Fallback for unknown difficulties

static func get_difficulty_name(difficulty: Difficulty) -> String:
	match difficulty:
		Difficulty.EASY:
			return "Easy"
		Difficulty.NORMAL:
			return "Normal"
		Difficulty.HARD:
			return "Hard"
		Difficulty.EXPERT:
			return "Expert"
		Difficulty.EXPERT_PLUS:
			return "ExpertPlus"
		_:
			return "Easy" # TODO: Fallback for unknown difficulties

static func get_file_name(difficulty: Difficulty, mode: BeatMapDifficultySet.BeatmapMode) -> String:
	return "%s%s.dat" % [get_difficulty_name(difficulty), BeatMapDifficultySet.get_mode_name(mode)]
