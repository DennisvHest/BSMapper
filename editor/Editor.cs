using Godot;

[GlobalClass]
public partial class Editor : Node3D
{
    private static readonly StringName AxButton = "ax_button";
    private static readonly StringName HapticAction = "haptic";
    private const int LaserShow = 1;
    private const int LaserHide = 2;

    [Export(PropertyHint.File, "*.dat")]
    public string BeatmapFilePath { get; set; } = string.Empty;

    private XROrigin3D _xrOrigin;
    private XRController3D _leftHand;
    private XRController3D _rightHand;
    private GodotObject _leftPointer;
    private GodotObject _rightPointer;
    private Saber _leftSaber;
    private Saber _rightSaber;
    private AudioStreamPlayer _hitSound;
    private AudioStreamPlayer _badCutSound;
    private BeatmapObject _leftDraggedObject;
    private BeatmapObject _rightDraggedObject;

    private PlaybackManager PlaybackManager => GetNode<PlaybackManager>("/root/PlaybackManager");
    private BeatMapManager BeatMapManager => GetNode<BeatMapManager>("/root/BeatMapManager");

    public override void _Ready()
    {
        _xrOrigin = GetNode<XROrigin3D>("XROrigin3D");
        _leftHand = GetNode<XRController3D>("XROrigin3D/LeftHand");
        _rightHand = GetNode<XRController3D>("XROrigin3D/RightHand");
        _leftPointer = GetNode("XROrigin3D/LeftHand/FunctionPointer");
        _rightPointer = GetNode("XROrigin3D/RightHand/FunctionPointer");
        _leftSaber = GetNode<Saber>("XROrigin3D/LeftHand/Saber");
        _rightSaber = GetNode<Saber>("XROrigin3D/RightHand/Saber");
        _hitSound = GetNode<AudioStreamPlayer>("HitSound");
        _badCutSound = GetNode<AudioStreamPlayer>("BadCutSound");

        var cameraPosition = GetNode<XRCamera3D>("XROrigin3D/XRCamera3D").Position;
        cameraPosition.Y = GlobalSettings.PlayerHeight;
        GetNode<XRCamera3D>("XROrigin3D/XRCamera3D").Position = cameraPosition;

        var gameEvents = GetNode<GameEvents>("/root/GameEvents");
        gameEvents.NoteBlockHit += OnNoteBlockHit;
        gameEvents.BombHit += OnBombHit;
        _leftHand.ButtonPressed += OnLeftHandButtonPressed;
        _leftHand.ButtonReleased += OnLeftHandButtonReleased;
        _rightHand.ButtonPressed += OnRightHandButtonPressed;
        _rightHand.ButtonReleased += OnRightHandButtonReleased;

        PlaybackManager.Initialize();
        PlaybackManager.ModeChanged += OnPlaybackModeChanged;
        Callable.From(() => PlaybackManager.Play()).CallDeferred();
    }

    public override void _Process(double delta)
    {
        MoveDraggedObject(_leftPointer, _leftDraggedObject);
        MoveDraggedObject(_rightPointer, _rightDraggedObject);
    }

    private void OnLeftHandButtonPressed(string buttonName)
    {
        if (buttonName == AxButton)
        {
            DeleteHoveredObjectForPointer(_leftPointer);
        }
        else if (buttonName == "grip_click")
        {
            _leftDraggedObject = GetGrippableObject(_leftPointer);
        }
    }

    private void OnRightHandButtonPressed(string buttonName)
    {
        GD.Print($"Right hand button pressed {buttonName}");
        if (buttonName == AxButton && !DeleteHoveredObjectForPointer(_rightPointer))
        {
            BeatMapManager.SaveBeatmap();
        }
        else if (buttonName == "grip_click")
        {
            _rightDraggedObject = GetGrippableObject(_rightPointer);
        }
    }

    private void OnLeftHandButtonReleased(string buttonName)
    {
        if (buttonName == "grip_click")
        {
            _leftDraggedObject = null;
        }
    }

    private void OnRightHandButtonReleased(string buttonName)
    {
        if (buttonName == "grip_click")
        {
            _rightDraggedObject = null;
        }
    }

    private static bool DeleteHoveredObjectForPointer(GodotObject pointer)
    {
        var hoveredObject = GetHoveredBeatmapObject(pointer);
        if (hoveredObject is null)
        {
            return false;
        }

        hoveredObject.DeleteBeatmapObject();
        return true;
    }

    private static BeatmapObject GetHoveredBeatmapObject(GodotObject pointer)
    {
        var target = pointer.Get("target").AsGodotObject() as Node
            ?? pointer.Get("last_target").AsGodotObject() as Node;
        if (target is null)
        {
            return null;
        }

        return target as BeatmapObject ?? target.GetParent() as BeatmapObject;
    }

    private BeatmapObject GetGrippableObject(GodotObject pointer)
    {
        return PlaybackManager.Mode == PlaybackManager.EditMode.Editing
            ? GetHoveredBeatmapObject(pointer)
            : null;
    }

    private static void MoveDraggedObject(GodotObject pointer, BeatmapObject draggedObject)
    {
        if (draggedObject is null)
        {
            return;
        }

        var target = pointer.Get("last_target").AsGodotObject() as Node;
        for (var current = target; current is not null; current = current.GetParent())
        {
            if (current is ObjectEditPlaneCell cell)
            {
                draggedObject.MoveToGridCell(cell.LineIndex, cell.LineLayer);
                return;
            }
        }
    }

    private void OnPlaybackModeChanged()
    {
        var editing = PlaybackManager.Mode == PlaybackManager.EditMode.Editing;
        var originPosition = _xrOrigin.Position;
        originPosition.Z = editing ? 2.0f : 0.0f;
        _xrOrigin.Position = originPosition;

        SetPointerEditingEnabled(_leftPointer, editing);
        SetPointerEditingEnabled(_rightPointer, editing);
        _leftSaber.Visible = !editing;
        _rightSaber.Visible = !editing;
    }

    private static void SetPointerEditingEnabled(GodotObject pointer, bool enabled)
    {
        pointer.Call("set_enabled", enabled);
        pointer.Call("set_show_laser", enabled ? LaserShow : LaserHide);
    }

    private void OnNoteBlockHit(Saber.SaberType saberType)
    {
        // Hit sounds are offset so they do not feel like they play before the block is hit.
        _hitSound.Play(0.15f);
        TriggerSaberHapticPulse(saberType);
    }

    private void OnBombHit(Saber.SaberType saberType)
    {
        _badCutSound.Play();
        TriggerSaberHapticPulse(saberType);
    }

    private void TriggerSaberHapticPulse(Saber.SaberType saberType)
    {
        var hand = saberType == Saber.SaberType.Left ? _leftHand : _rightHand;
        hand.TriggerHapticPulse(HapticAction, 0.0f, 1.0f, 0.15f, 0.0f);
    }
}