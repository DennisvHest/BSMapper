extends Control

class_name ObjectTypeSelectorUI

signal placeable_selected(selected_object_type)

const PLACEABLE_NOTE_BLOCK := 0
const PLACEABLE_BOMB := 1
const SELECTED_BACKGROUND := Color(0.91, 0.53, 0.17, 1.0)
const IDLE_BACKGROUND := Color(0.17, 0.19, 0.24, 1.0)
const HOVER_BACKGROUND := Color(0.23, 0.26, 0.32, 1.0)
const PANEL_BACKGROUND := Color(0.05, 0.06, 0.08, 0.92)
const PANEL_BORDER := Color(0.98, 0.65, 0.24, 0.2)
const SELECTED_TEXT := Color(1.0, 1.0, 1.0, 1.0)
const IDLE_TEXT := Color(0.83, 0.86, 0.9, 1.0)

@onready var _panel: Panel = $Panel
@onready var _note_button: Button = %NoteButton
@onready var _bomb_button: Button = %BombButton

func _ready() -> void:
	_configure_panel()
	_configure_button(_note_button)
	_configure_button(_bomb_button)

	_note_button.pressed.connect(_on_note_button_pressed)
	_bomb_button.pressed.connect(_on_bomb_button_pressed)

	set_selected_object_type(PLACEABLE_NOTE_BLOCK)

func set_selected_object_type(selected_object_type: int) -> void:
	_apply_button_state(_note_button, selected_object_type == PLACEABLE_NOTE_BLOCK)
	_apply_button_state(_bomb_button, selected_object_type == PLACEABLE_BOMB)

func _on_note_button_pressed() -> void:
	placeable_selected.emit(PLACEABLE_NOTE_BLOCK)

func _on_bomb_button_pressed() -> void:
	placeable_selected.emit(PLACEABLE_BOMB)

func _configure_panel() -> void:
	var panel_style := StyleBoxFlat.new()
	panel_style.bg_color = PANEL_BACKGROUND
	panel_style.corner_radius_top_left = 20
	panel_style.corner_radius_top_right = 20
	panel_style.corner_radius_bottom_right = 20
	panel_style.corner_radius_bottom_left = 20
	panel_style.border_width_left = 2
	panel_style.border_width_top = 2
	panel_style.border_width_right = 2
	panel_style.border_width_bottom = 2
	panel_style.border_color = PANEL_BORDER
	panel_style.shadow_size = 8
	panel_style.shadow_color = Color(0, 0, 0, 0.28)
	_panel.add_theme_stylebox_override("panel", panel_style)

func _configure_button(button: Button) -> void:
	button.flat = true
	button.toggle_mode = false
	button.add_theme_font_size_override("font_size", 56)
	button.add_theme_constant_override("outline_size", 10)
	button.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.45))
	button.add_theme_constant_override("h_separation", 0)
	button.focus_mode = Control.FOCUS_NONE

func _apply_button_state(button: Button, is_selected: bool) -> void:
	var background := SELECTED_BACKGROUND if is_selected else IDLE_BACKGROUND
	var hover_background := SELECTED_BACKGROUND if is_selected else HOVER_BACKGROUND
	var text_color := SELECTED_TEXT if is_selected else IDLE_TEXT

	button.add_theme_stylebox_override("normal", _build_button_style(background))
	button.add_theme_stylebox_override("hover", _build_button_style(hover_background))
	button.add_theme_stylebox_override("pressed", _build_button_style(SELECTED_BACKGROUND))
	button.add_theme_stylebox_override("focus", _build_button_style(hover_background))
	button.add_theme_stylebox_override("disabled", _build_button_style(background))
	button.add_theme_color_override("font_color", text_color)
	button.add_theme_color_override("font_hover_color", text_color)
	button.add_theme_color_override("font_pressed_color", SELECTED_TEXT)
	button.add_theme_color_override("font_focus_color", text_color)

func _build_button_style(background: Color) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = background
	style.corner_radius_top_left = 16
	style.corner_radius_top_right = 16
	style.corner_radius_bottom_right = 16
	style.corner_radius_bottom_left = 16
	style.border_width_left = 2
	style.border_width_top = 2
	style.border_width_right = 2
	style.border_width_bottom = 2
	style.border_color = Color(1, 1, 1, 0.07)
	style.content_margin_left = 12
	style.content_margin_top = 18
	style.content_margin_right = 12
	style.content_margin_bottom = 18
	return style