extends Node

var checks := 0
var failed := false
var opened_paths: Array[String] = []

func _ready() -> void:
	call_deferred("run")

func check(condition: bool, description: String) -> void:
	checks += 1
	if not condition:
		failed = true
		push_error(description)

func write(path: String, text: String) -> void:
	var file := FileAccess.open(path, FileAccess.WRITE)
	file.store_string(text)

func edit(field: LineEdit, text: String) -> void:
	field.text = text
	field.text_changed.emit(text)

func fully_visible(control: Control) -> bool:
	var bounds := control.get_global_rect()
	var visible_area := control.get_viewport_rect()
	var ancestor := control.get_parent()
	while ancestor != null:
		if ancestor is Control and ancestor.clip_contents:
			visible_area = visible_area.intersection(ancestor.get_global_rect())
		ancestor = ancestor.get_parent()
	var result := control.is_visible_in_tree() and bounds.has_area() and visible_area.encloses(bounds)
	if not result:
		print("Warning bounds: %s; visible area: %s; form: %s" % [bounds, visible_area, control.get_parent().get_global_rect()])
	return result

func run() -> void:
	# Headless windows default to 64x64; use a desktop-sized logical viewport.
	get_tree().root.content_scale_size = Vector2i(1152, 720)
	get_tree().root.content_scale_mode = Window.CONTENT_SCALE_MODE_CANVAS_ITEMS
	# Only create and remove files in this test's isolated fixture directory.
	var folder := ProjectSettings.globalize_path("res://tests/map-details-%s" % Time.get_ticks_usec())
	DirAccess.make_dir_absolute(folder)
	var info_path := folder.path_join("info.dat")
	var difficulty_path := folder.path_join("EasyStandard.dat")
	var audio_path := folder.path_join("song.ogg")
	var fixture := {
		"_version": "2.0.0", "_songName": "Original", "_songSubName": "",
		"_songAuthorName": "Author", "_levelAuthorName": "", "_songFilename": "song.ogg",
		"_coverImageFilename": "", "_beatsPerMinute": 120, "_previewStartTime": 0,
		"_previewDuration": 0, "_songTimeOffset": 0,
		"_difficultyBeatmapSets": [{"_beatmapCharacteristicName": "Standard", "_difficultyBeatmaps": [{
			"_difficulty": "Easy", "_beatmapFilename": "EasyStandard.dat",
			"_noteJumpMovementSpeed": 10, "_noteJumpStartBeatOffset": 0}]}]
	}
	var objects := '{"_version":"2.0.0","_notes":[{"_type":3}],"_obstacles":[],"_events":[]}'
	write(info_path, JSON.stringify(fixture))
	write(difficulty_path, objects)
	write(audio_path, "original audio")
	var original_info := FileAccess.get_file_as_string(info_path)
	var manager = get_node("/root/BeatMapManager")
	var info = manager.ReadBeatmapInfo(info_path)
	var details = load("res://ui/map_details.tscn").instantiate()
	add_child(details)
	details.Populate(manager, info)
	var save: Button = details.get_node("%SaveMap")
	var warning: Label = details.get_node("%DifficultyDeletionWarning")
	var name_field: LineEdit = details.get_node("%SongName")
	var easy: CheckBox = details.get_node("%Difficulties").get_child(0)
	check(not save.visible and not warning.visible, "Loading a map starts clean")
	edit(name_field, "Changed")
	name_field.text_submitted.emit(name_field.text)
	name_field.focus_exited.emit()
	check(save.visible, "Typing shows Save")
	check(FileAccess.get_file_as_string(info_path) == original_info and info.SongName == "Original",
		"Typing, submitting and leaving metadata do not save or mutate the model")
	edit(name_field, "Original")
	check(not save.visible, "Reverting metadata hides Save")
	easy.button_pressed = false
	check(save.visible and warning.visible and "Easy" in warning.text and "notes, bombs, walls" in warning.text,
		"Unchecking warns about difficulty data deletion")
	await get_tree().process_frame
	await get_tree().process_frame
	await get_tree().process_frame
	check(fully_visible(warning), "Deletion warning is fully visible without scrolling after unchecking Easy")
	check(FileAccess.get_file_as_string(difficulty_path) == objects and FileAccess.get_file_as_string(info_path) == original_info,
		"Unchecking preserves files until Save")
	easy.button_pressed = true
	check(not save.visible and not warning.visible, "Rechecking cancels deletion and clears dirty state")
	var njs: SpinBox = easy.get_node("%Njs")
	# Exercise typing, rather than only the spinbox arrow buttons.
	njs.get_line_edit().grab_focus()
	njs.get_line_edit().text = "12"
	njs.get_line_edit().text_changed.emit("12")
	await get_tree().process_frame
	check(save.visible and info.FindDifficulty(0, 0).Njs == 10, "Difficulty values are staged while typing")
	save.pressed.emit()
	check(not save.visible and manager.ReadBeatmapInfo(info_path).FindDifficulty(0, 0).Njs == 12,
		"Save persists difficulty values and clears dirty state")
	check(FileAccess.get_file_as_string(difficulty_path) == objects, "Updating settings preserves difficulty objects")
	var bpm: LineEdit = details.get_node("%Bpm")
	edit(bpm, "invalid")
	var before_invalid_save := FileAccess.get_file_as_string(info_path)
	save.pressed.emit()
	check(save.visible and details.get_node("%Error").visible and FileAccess.get_file_as_string(info_path) == before_invalid_save,
		"Invalid Save retains changes and reports validation error")
	edit(bpm, "120")
	var replacement_audio := folder.path_join("replacement.ogg")
	write(replacement_audio, "new audio")
	details.get_node("%AudioFileDialog").file_selected.emit(replacement_audio)
	check(save.visible and FileAccess.get_file_as_string(audio_path) == "original audio", "Audio selection is staged")
	var cover_path := folder.path_join("replacement.png")
	var image := Image.create_empty(2, 2, false, Image.FORMAT_RGBA8)
	image.fill(Color.RED)
	image.save_png(cover_path)
	details.get_node("%CoverImageFileDialog").file_selected.emit(cover_path)
	check(info.CoverImageFileName == "" and not FileAccess.file_exists(folder.path_join("cover.png")),
		"Cover selection is staged")
	edit(name_field, "Saved title")
	save.pressed.emit()
	check(not save.visible and details.visible, "Successful Save clears changes and keeps details visible")
	check(FileAccess.get_file_as_string(audio_path) == "new audio" and FileAccess.file_exists(folder.path_join("cover.png"))
		and manager.ReadBeatmapInfo(info_path).SongName == "Saved title", "Save persists metadata, audio and cover")
	var unsaved: ConfirmationDialog = details.get_node("%UnsavedChangesDialog")
	var open: Button = details.get_node("%OpenMap")
	details.OpenMapRequested.connect(func(path: String): opened_paths.append(path))
	open.pressed.emit()
	check(opened_paths.size() == 1 and not unsaved.visible, "Clean maps open without a warning")
	edit(name_field, "Pending VR title")
	open.pressed.emit()
	check(unsaved.visible and opened_paths.size() == 1, "Unsaved edits block opening VR and show the dialog")
	unsaved.canceled.emit()
	check(not unsaved.visible and name_field.text == "Pending VR title" and save.visible
		and opened_paths.size() == 1, "Cancel keeps edits and does not open VR")
	open.pressed.emit()
	unsaved.custom_action.emit("discard")
	check(opened_paths.size() == 2 and name_field.text == "Saved title" and not save.visible
		and manager.ReadBeatmapInfo(info_path).SongName == "Saved title", "Discard opens VR using saved details")
	edit(name_field, "Saved before VR")
	open.pressed.emit()
	unsaved.confirmed.emit()
	check(opened_paths.size() == 3 and manager.ReadBeatmapInfo(info_path).SongName == "Saved before VR"
		and not save.visible, "Save and continue persists changes before opening VR")
	edit(bpm, "invalid")
	open.pressed.emit()
	unsaved.confirmed.emit()
	check(opened_paths.size() == 3 and save.visible and details.get_node("%Error").visible,
		"Validation failure prevents opening VR and keeps pending edits")
	edit(bpm, "120")
	var other_path := folder.path_join("other-info.dat")
	fixture["_songName"] = "Other map"
	write(other_path, JSON.stringify(fixture))
	edit(name_field, "Pending switch title")
	details.Populate(manager, manager.ReadBeatmapInfo(other_path))
	check(unsaved.visible and name_field.text == "Pending switch title", "Switching maps waits for the unsaved dialog")
	unsaved.canceled.emit()
	check(name_field.text == "Pending switch title" and not unsaved.visible, "Cancel map switch preserves the current form")
	details.Populate(manager, manager.ReadBeatmapInfo(other_path))
	unsaved.confirmed.emit()
	check(name_field.text == "Other map" and manager.ReadBeatmapInfo(info_path).SongName == "Pending switch title",
		"Save and continue saves the previous map before switching")
	edit(name_field, "Discard this")
	details.Populate(manager, manager.ReadBeatmapInfo(info_path))
	unsaved.custom_action.emit("discard")
	check(name_field.text == "Pending switch title" and manager.ReadBeatmapInfo(other_path).SongName == "Other map",
		"Discard switches maps without saving edits to the previous map")
	edit(name_field, "Same map save")
	details.Populate(manager, manager.ReadBeatmapInfo(info_path))
	unsaved.confirmed.emit()
	check(name_field.text == "Same map save" and not save.visible, "Reselecting the same map loads the newly saved data")
	edit(name_field, "Keep while creating")
	details.BeginCreate(manager)
	check(unsaved.visible and name_field.text == "Keep while creating", "Starting a new map warns about existing edits")
	unsaved.canceled.emit()
	edit(name_field, "Same map save")
	easy.button_pressed = false
	open.pressed.emit()
	check(unsaved.visible and "permanently delete" in unsaved.dialog_text, "Unsaved dialog includes pending difficulty deletion warning")
	unsaved.canceled.emit()
	save.pressed.emit()
	check(not FileAccess.file_exists(difficulty_path) and not save.visible and not warning.visible
		and manager.ReadBeatmapInfo(info_path).DifficultyBeatMapSets.size() == 0,
		"Save deletes the unchecked difficulty and clears the warning")
	easy.button_pressed = true
	check(save.visible and not warning.visible, "Adding a new difficulty has no deletion warning")
	details.Populate(manager, manager.ReadBeatmapInfo(info_path))
	check(unsaved.visible and easy.button_pressed, "Pending difficulty additions also block map switching")
	unsaved.custom_action.emit("discard")
	check(not save.visible and not warning.visible and not easy.button_pressed, "Reload resets pending changes")
	details.BeginCreate(manager)
	edit(name_field, "New map")
	easy.button_pressed = true
	check(not save.visible and not warning.visible and details.get_node("%CreateMap").visible,
		"New maps continue to use Create map")
	details.Populate(manager, manager.ReadBeatmapInfo(info_path))
	check(unsaved.visible and name_field.text == "New map", "Unfinished new maps are protected when switching")
	unsaved.confirmed.emit()
	check(name_field.text == "New map" and details.get_node("%Error").visible,
		"An incomplete new map cannot be saved and left")
	details.Populate(manager, manager.ReadBeatmapInfo(info_path))
	unsaved.custom_action.emit("discard")
	check(name_field.text == "Same map save" and not unsaved.visible, "Discard leaves an unfinished new map")
	details.queue_free()
	for file in DirAccess.get_files_at(folder):
		DirAccess.remove_absolute(folder.path_join(file))
	DirAccess.remove_absolute(folder)
	print("Map details checks: %s passed, failed=%s" % [checks, failed])
	get_tree().quit(1 if failed else 0)
