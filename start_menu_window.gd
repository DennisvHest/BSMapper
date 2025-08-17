extends CanvasLayer

@export var editor_scene: PackedScene

func _on_start_button_pressed() -> void:
    # Change to the main scene when the start button is pressed
    get_tree().change_scene_to_packed(editor_scene)