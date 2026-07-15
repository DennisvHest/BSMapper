using Godot;

public partial class GameEvents : Node
{
    [Signal]
    public delegate void NoteBlockHitEventHandler(Saber.SaberType saberType);

    [Signal]
    public delegate void BombHitEventHandler(Saber.SaberType saberType);
}