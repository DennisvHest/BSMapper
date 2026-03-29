extends Control

class_name ObjectTypeSelectorUI

signal placeable_selected(selected_object_type)

const PLACEABLE_NOTE_BLOCK := 0
const PLACEABLE_BOMB := 1

@export var idle_button_style: StyleBox
@export var hover_button_style: StyleBox
@export var selected_button_style: StyleBox
@export var idle_text_color := Color(0.83, 0.86, 0.9, 1.0)
@export var selected_text_color := Color(1.0, 1.0, 1.0, 1.0)

@onready var _note_button: Button = %NoteButton
@onready var _bomb_button: Button = %BombButton

func _ready() -> void:
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

func _apply_button_state(button: Button, is_selected: bool) -> void:
	var normal_style := selected_button_style if is_selected else idle_button_style
	var active_hover_style := selected_button_style if is_selected else hover_button_style
	var text_color := selected_text_color if is_selected else idle_text_color

	button.add_theme_stylebox_override("normal", normal_style)
	button.add_theme_stylebox_override("hover", active_hover_style)
	button.add_theme_stylebox_override("pressed", selected_button_style)
	button.add_theme_stylebox_override("focus", active_hover_style)
	button.add_theme_stylebox_override("disabled", normal_style)
	button.add_theme_color_override("font_color", text_color)
	button.add_theme_color_override("font_hover_color", text_color)
	button.add_theme_color_override("font_pressed_color", selected_text_color)
	button.add_theme_color_override("font_focus_color", text_color)
