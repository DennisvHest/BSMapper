using Godot;
using System;

public partial class InputManager : Node
{
    public XRController3D LeftHand { get; private set; }
    public XRController3D RightHand { get; private set; }

    public void Initialize()
    {
        LeftHand = GetParent().GetNode<XRController3D>("Editor/XROrigin3D/LeftHand");
        RightHand = GetParent().GetNode<XRController3D>("Editor/XROrigin3D/RightHand");
    }
}

public static class Inputs
{
    public static readonly StringName AxButton = "ax_button";
    public static readonly StringName GripButton = "grip_click";
    public static readonly StringName TriggerButton = "trigger_click";
    public static readonly StringName Haptic = "haptic";
}

public static class InputActions
{
    public static readonly StringName ToggleEditMode = Inputs.AxButton;
    public static readonly StringName DeleteObject = Inputs.AxButton;
    public static readonly StringName SelectObject = Inputs.TriggerButton;
    public static readonly StringName ToggleSelectionMode = Inputs.GripButton;
    public static readonly StringName SaveMap = Inputs.AxButton;
    public static readonly StringName MoveObject = Inputs.GripButton;
}
