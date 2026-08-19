using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class Editor : Node3D
{
    private const int LaserShow = 1;
    private const int LaserHide = 2;

    [Export(PropertyHint.File, "*.dat")]
    public string BeatmapFilePath { get; set; } = string.Empty;

    public event Action<int, bool> SelectionChanged;

    private XROrigin3D _xrOrigin;
    private XRController3D _leftHand;
    private XRController3D _rightHand;
    private GodotObject _leftPointer;
    private GodotObject _rightPointer;
    private Saber _leftSaber;
    private Saber _rightSaber;
    private AudioStreamPlayer _hitSound;
    private AudioStreamPlayer _badCutSound;
    private ObjectEditPlane _objectEditPlane;
    private DragState _rightDrag;
    private readonly HashSet<BeatmapObject> _selectedObjects = new();
    private bool _leftSelectionMode;

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
        _objectEditPlane = GetNode<ObjectEditPlane>("NoteBlockLane/ObjectEditPlane");

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
        MoveDraggedObject(_rightPointer, _rightHand, _rightDrag);
    }

    private void OnLeftHandButtonPressed(string buttonName)
    {
        if (buttonName == InputActions.SelectObject && _leftSelectionMode)
        {
            ToggleHoveredObjectSelection(_leftPointer);
        }
        else if (buttonName == InputActions.DeleteObject)
        {
            DeleteHoveredObjectForPointer(_leftPointer);
        }
        else if (buttonName == InputActions.ToggleSelectionMode && PlaybackManager.Mode == PlaybackManager.EditMode.Editing)
        {
            _leftSelectionMode = true;
            _objectEditPlane.SetSelectionModeEnabled(true);
        }
    }

    private void OnRightHandButtonPressed(string buttonName)
    {
        GD.Print($"Right hand button pressed {buttonName}");
        if (buttonName == InputActions.SelectObject && _leftSelectionMode)
        {
            ToggleHoveredObjectSelection(_rightPointer);
        }
        else if (buttonName == InputActions.SaveMap && !DeleteHoveredObjectForPointer(_rightPointer))
        {
            BeatMapManager.SaveBeatmap();
        }
        else if (buttonName == InputActions.MoveObject)
        {
            _rightDrag = CreateDragState(_rightPointer, _rightHand);
        }
    }

    private void OnLeftHandButtonReleased(string buttonName)
    {
        if (buttonName == InputActions.ToggleSelectionMode)
        {
            _leftSelectionMode = false;
            _objectEditPlane.SetSelectionModeEnabled(false);
        }
    }

    private void OnRightHandButtonReleased(string buttonName)
    {
        if (buttonName == InputActions.MoveObject)
        {
            _rightDrag = null;
        }
    }

    public void SetSelectedNoteBlockType(BeatMapNote.NoteBlockType type)
    {
        foreach (var selectedObject in _selectedObjects)
        {
            if (selectedObject is NoteBlock noteBlock)
            {
                noteBlock.SetNoteBlockType(type);
            }
        }
    }

    public void DeleteSelectedObjects()
    {
        var objectsToDelete = new List<BeatmapObject>(_selectedObjects);
        _selectedObjects.Clear();
        foreach (var selectedObject in objectsToDelete)
        {
            selectedObject.SetSelected(false);
            selectedObject.DeleteBeatmapObject();
        }

        EmitSelectionChanged();
    }

    public void DeselectAllObjects()
    {
        var selectedObjects = new List<BeatmapObject>(_selectedObjects);
        _selectedObjects.Clear();
        foreach (var selectedObject in selectedObjects)
        {
            selectedObject.SetSelected(false);
        }

        EmitSelectionChanged();
    }

    private bool DeleteHoveredObjectForPointer(GodotObject pointer)
    {
        var hoveredObject = GetHoveredBeatmapObject(pointer);
        if (hoveredObject is null)
        {
            return false;
        }

        RemoveSelectedObject(hoveredObject);
        hoveredObject.DeleteBeatmapObject();
        return true;
    }

    private void ToggleHoveredObjectSelection(GodotObject pointer)
    {
        var hoveredObject = GetHoveredBeatmapObject(pointer);
        if (hoveredObject is null)
        {
            return;
        }

        if (_selectedObjects.Remove(hoveredObject))
        {
            hoveredObject.SetSelected(false);
        }
        else
        {
            _selectedObjects.Add(hoveredObject);
            hoveredObject.SetSelected(true);
            hoveredObject.TreeExiting += () => RemoveSelectedObject(hoveredObject);
        }

        EmitSelectionChanged();
    }

    private void RemoveSelectedObject(BeatmapObject beatmapObject)
    {
        if (!_selectedObjects.Remove(beatmapObject))
        {
            return;
        }

        beatmapObject.SetSelected(false);
        EmitSelectionChanged();
    }

    private void EmitSelectionChanged()
    {
        var containsNotes = false;
        foreach (var selectedObject in _selectedObjects)
        {
            if (selectedObject is NoteBlock)
            {
                containsNotes = true;
                break;
            }
        }

        SelectionChanged?.Invoke(_selectedObjects.Count, containsNotes);
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

    private DragState CreateDragState(GodotObject pointer, XRController3D controller)
    {
        var draggedObject = GetGrippableObject(pointer);
        return draggedObject is null
            ? null
            : new DragState(
                draggedObject,
                controller.GlobalRotation.Z,
                draggedObject.BeatmapData is BeatMapNote note ? GetCutDirectionRotation(note.Cut) : 0.0f);
    }

    private static void MoveDraggedObject(GodotObject pointer, XRController3D controller, DragState drag)
    {
        if (drag is null)
        {
            return;
        }

        var target = pointer.Get("last_target").AsGodotObject() as Node;
        for (var current = target; current is not null; current = current.GetParent())
        {
            if (current is ObjectEditPlaneCell cell)
            {
                drag.Object.MoveToGridCell(cell.LineIndex, cell.LineLayer);
                break;
            }
        }

        if (drag.Object is NoteBlock noteBlock)
        {
            var rotation = Mathf.PosMod(
                controller.GlobalRotation.Z - drag.StartingControllerRoll + Mathf.Pi,
                Mathf.Tau) - Mathf.Pi + drag.StartingCutDirectionRotation;
            noteBlock.SetCutDirection(GetCutDirectionFromRotation(rotation));
        }
    }

    private static BeatMapNote.CutDirection GetCutDirectionFromRotation(float rotation)
    {
        var directionIndex = Mathf.PosMod(Mathf.RoundToInt(rotation / (Mathf.Pi / 4.0f)), 8);
        return directionIndex switch
        {
            1 => BeatMapNote.CutDirection.DownRight,
            2 => BeatMapNote.CutDirection.Right,
            3 => BeatMapNote.CutDirection.UpRight,
            4 => BeatMapNote.CutDirection.Up,
            5 => BeatMapNote.CutDirection.UpLeft,
            6 => BeatMapNote.CutDirection.Left,
            7 => BeatMapNote.CutDirection.DownLeft,
            _ => BeatMapNote.CutDirection.Down,
        };
    }

    private static float GetCutDirectionRotation(BeatMapNote.CutDirection cutDirection)
    {
        return cutDirection switch
        {
            BeatMapNote.CutDirection.DownRight => Mathf.Pi / 4.0f,
            BeatMapNote.CutDirection.Right => Mathf.Pi / 2.0f,
            BeatMapNote.CutDirection.UpRight => Mathf.Pi * 3.0f / 4.0f,
            BeatMapNote.CutDirection.Up => Mathf.Pi,
            BeatMapNote.CutDirection.UpLeft => -Mathf.Pi * 3.0f / 4.0f,
            BeatMapNote.CutDirection.Left => -Mathf.Pi / 2.0f,
            BeatMapNote.CutDirection.DownLeft => -Mathf.Pi / 4.0f,
            _ => 0.0f,
        };
    }

    private sealed class DragState
    {
        public DragState(
            BeatmapObject @object,
            float startingControllerRoll,
            float startingCutDirectionRotation)
        {
            Object = @object;
            StartingControllerRoll = startingControllerRoll;
            StartingCutDirectionRotation = startingCutDirectionRotation;
        }

        public BeatmapObject Object { get; }
        public float StartingControllerRoll { get; }
        public float StartingCutDirectionRotation { get; }
    }

    private void OnPlaybackModeChanged()
    {
        var editing = PlaybackManager.Mode == PlaybackManager.EditMode.Editing;
        if (!editing)
        {
            _leftSelectionMode = false;
            _rightDrag = null;
            _objectEditPlane.SetSelectionModeEnabled(false);
        }
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
        hand.TriggerHapticPulse(Inputs.Haptic, 0.0f, 1.0f, 0.15f, 0.0f);
    }
}