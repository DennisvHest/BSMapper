using Godot;
using System;
using System.Xml.Linq;

public partial class InputManager : Node
{
    private const float PlaybackScrubVelocityMultiplier = 0.01f;

    public XRController3D LeftHand { get; private set; }
    public XRController3D RightHand { get; private set; }

    public Node LeftHandPointer { get; private set; }
    public Node RightHandPointer { get; private set; }

    private PlaybackManager _playbackManager;

    public bool Initialized { get; private set; }

    public override void _Ready()
    {
        _playbackManager = GetNode<PlaybackManager>("/root/PlaybackManager");
    }

    public void Initialize()
    {
        LeftHand = GetParent().GetNode<XRController3D>("Editor/XROrigin3D/LeftHand");
        LeftHand.ButtonPressed += OnLeftHandButtonPressed;
        LeftHand.InputVector2Changed += OnLeftHandInputVector2Changed;

        RightHand = GetParent().GetNode<XRController3D>("Editor/XROrigin3D/RightHand");
        RightHand.ButtonPressed += OnRightHandButtonPressed;

        LeftHandPointer = LeftHand.GetNode("FunctionPointer");
        RightHandPointer = RightHand.GetNode("FunctionPointer");

        Initialized = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Initialized)
            return;

        var leftJoystickPosition = LeftHand.GetVector2("primary");
        _playbackManager.SetPlaybackScrubVelocity(leftJoystickPosition.X * PlaybackScrubVelocityMultiplier);
    }

    private void OnLeftHandButtonPressed(string actionName)
    {
        if (actionName == InputActions.ToggleEditMode)
        {
            _playbackManager.ToggleMode();
        }
        else if (actionName == InputActions.StepPlaybackSubdivision)
        {
            _playbackManager.StepBeatSubdivision(-1);
        }
    }

    private void OnRightHandButtonPressed(string actionName)
    {
        if (actionName == InputActions.StepPlaybackSubdivision)
        {
            _playbackManager.StepBeatSubdivision(1);
        }
    }

    private void OnLeftHandInputVector2Changed(string actionName, Vector2 value)
    {
        if (actionName != InputActions.ScrubPlayback)
            return;

        if (value.X != 0.0f)
        {
            _playbackManager.Pause();
        }
        else
        {
            _playbackManager.SetPlaybackScrubVelocity(0.0f);
            _playbackManager.SnapToNearestBeat();
        }
    }
}

public static class Inputs
{
    public static readonly StringName AxButton = "ax_button";
    public static readonly StringName GripButton = "grip_click";
    public static readonly StringName TriggerButton = "trigger_click";
    public static readonly StringName PrimaryClick = "primary_click";
    public static readonly StringName Haptic = "haptic";
    public static readonly StringName Primary = "primary"; // The primary joystick on the controller
}

public static class InputActions
{
    public static readonly StringName ToggleEditMode = Inputs.AxButton;
    public static readonly StringName DeleteObject = Inputs.AxButton;
    public static readonly StringName SelectObject = Inputs.TriggerButton;
    public static readonly StringName ToggleSelectionMode = Inputs.GripButton;
    public static readonly StringName SaveMap = Inputs.AxButton;
    public static readonly StringName MoveObject = Inputs.GripButton;
    public static readonly StringName ScrubPlayback = Inputs.Primary;
    public static readonly StringName StepPlaybackSubdivision = Inputs.PrimaryClick;
}
