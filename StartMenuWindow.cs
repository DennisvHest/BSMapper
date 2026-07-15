using Godot;

public partial class StartMenuWindow : CanvasLayer
{
    [Export]
    public PackedScene EditorScene { get; set; }

    private void OnStartButtonPressed()
    {
        GetTree().ChangeSceneToPacked(EditorScene);
    }
}