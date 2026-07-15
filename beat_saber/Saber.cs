using Godot;

[GlobalClass]
public partial class Saber : Node3D
{
    public enum SaberType
    {
        Left,
        Right,
    }

    [Export]
    public SaberType Type { get; set; }
}