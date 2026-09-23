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
    public event Action<int> ClipboardChanged;

    public int ClipboardCount => _clipboard.Count;

    private XROrigin3D _xrOrigin;
    private Saber _leftSaber;
    private Saber _rightSaber;
    private AudioStreamPlayer _hitSound;
    private AudioStreamPlayer _badCutSound;
    private ObjectEditPlane _objectEditPlane;
    private DragState _rightDrag;
    private readonly HashSet<BeatmapObject> _selectedObjects = new();
    private readonly HashSet<BeatmapObject> _bulkSelectedObjects = new();
    private readonly HashSet<BeatmapObject> _observedObjects = new();
    private readonly HashSet<BeatmapObject> _copiedObjects = new();
    private readonly HashSet<BeatmapObject> _cutObjects = new();
    private readonly BeatmapClipboard _clipboard = new();
    private bool _leftSelectionMode;

    private PlaybackManager PlaybackManager => GetNode<PlaybackManager>("/root/PlaybackManager");
    private BeatMapManager BeatMapManager => GetNode<BeatMapManager>("/root/BeatMapManager");
    private InputManager InputManager => GetNode<InputManager>("/root/InputManager");

    public override void _Ready()
    {
        _xrOrigin = GetNode<XROrigin3D>("XROrigin3D");
        _leftSaber = GetNode<Saber>("XROrigin3D/LeftHand/Saber");
        _rightSaber = GetNode<Saber>("XROrigin3D/RightHand/Saber");
        _hitSound = GetNode<AudioStreamPlayer>("HitSound");
        _badCutSound = GetNode<AudioStreamPlayer>("BadCutSound");
        _objectEditPlane = GetNode<ObjectEditPlane>("NoteBlockLane/ObjectEditPlane");
        _objectEditPlane.BulkSelectionModeChanged += OnBulkSelectionModeChanged;
        BeatMapManager.CurrentBeatmapChanged += OnCurrentBeatmapChanged;

        var cameraPosition = GetNode<XRCamera3D>("XROrigin3D/XRCamera3D").Position;
        cameraPosition.Y = GlobalSettings.PlayerHeight;
        GetNode<XRCamera3D>("XROrigin3D/XRCamera3D").Position = cameraPosition;

        var gameEvents = GetNode<GameEvents>("/root/GameEvents");
        gameEvents.NoteBlockHit += OnNoteBlockHit;
        gameEvents.BombHit += OnBombHit;

        InputManager.Initialize();
        InputManager.LeftHand.ButtonPressed += OnLeftHandButtonPressed;
        InputManager.LeftHand.ButtonReleased += OnLeftHandButtonReleased;
        InputManager.RightHand.ButtonPressed += OnRightHandButtonPressed;
        InputManager.RightHand.ButtonReleased += OnRightHandButtonReleased;

        PlaybackManager.Initialize();
        PlaybackManager.ModeChanged += OnPlaybackModeChanged;
        Callable.From(() => PlaybackManager.Play()).CallDeferred();
    }

    public override void _Process(double delta)
    {
        MoveDraggedObject(InputManager.RightHandPointer, InputManager.RightHand, _rightDrag);
    }

    private void OnLeftHandButtonPressed(string buttonName)
    {
        if (_objectEditPlane.BulkSelectionModeEnabled)
        {
            if (buttonName == InputActions.SelectObject)
            {
                _objectEditPlane.BeginBulkSelectionDrag(InputManager.LeftHandPointer);
            }
            return;
        }

        if (buttonName == InputActions.SelectObject && _leftSelectionMode)
        {
            ToggleHoveredObjectSelection(InputManager.LeftHandPointer);
        }
        else if (buttonName == InputActions.DeleteObject)
        {
            DeleteHoveredObjectForPointer(InputManager.LeftHandPointer);
        }
        else if (buttonName == InputActions.ToggleSelectionMode && PlaybackManager.Mode == PlaybackManager.EditMode.Editing)
        {
            _leftSelectionMode = true;
            _objectEditPlane.SetSelectionModeEnabled(true);
        }
    }

    private void OnRightHandButtonPressed(string buttonName)
    {
        if (_objectEditPlane.BulkSelectionModeEnabled)
        {
            if (buttonName == InputActions.SelectObject)
            {
                _objectEditPlane.BeginBulkSelectionDrag(InputManager.RightHandPointer);
            }
            return;
        }

        if (buttonName == InputActions.SelectObject && _leftSelectionMode)
        {
            ToggleHoveredObjectSelection(InputManager.RightHandPointer);
        }
        else if (buttonName == InputActions.SaveMap && !DeleteHoveredObjectForPointer(InputManager.RightHandPointer))
        {
            BeatMapManager.SaveBeatmap();
        }
        else if (buttonName == InputActions.MoveObject)
        {
            _rightDrag = CreateDragState(InputManager.RightHandPointer, InputManager.RightHand);
        }
    }

    private void OnLeftHandButtonReleased(string buttonName)
    {
        if (buttonName == InputActions.SelectObject)
        {
            _objectEditPlane.EndBulkSelectionDrag(InputManager.LeftHandPointer);
        }

        if (buttonName == InputActions.ToggleSelectionMode)
        {
            _leftSelectionMode = false;
            _objectEditPlane.SetSelectionModeEnabled(false);
        }
    }

    private void OnRightHandButtonReleased(string buttonName)
    {
        if (buttonName == InputActions.SelectObject)
        {
            _objectEditPlane.EndBulkSelectionDrag(InputManager.RightHandPointer);
        }

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

    public void SetSelectedNoteCutDirection(BeatMapNote.CutDirection cutDirection)
    {
        foreach (var selectedObject in _selectedObjects)
        {
            if (selectedObject is NoteBlock noteBlock)
            {
                noteBlock.SetCutDirection(cutDirection);
            }
        }
    }

    public void MoveSelectedObjectsBySubdivision(int direction)
    {
        if (direction == 0 || PlaybackManager.BeatSubdivision <= 0)
        {
            return;
        }

        var beatOffset = direction / (double)PlaybackManager.BeatSubdivision;
        foreach (var selectedObject in _selectedObjects)
        {
            selectedObject.MoveByBeat(beatOffset);
        }
    }

    public void CopySelectedObjects()
    {
        StartClipboardFromSelection(false);
    }

    public void CutSelectedObjects()
    {
        StartClipboardFromSelection(true);
    }

    private void StartClipboardFromSelection(bool cut)
    {
        var sources = new List<BeatmapObject>();
        var data = new List<BeatMapObjectBase>();
        foreach (var selectedObject in _selectedObjects)
        {
            if (IsInstanceValid(selectedObject) && !selectedObject.IsQueuedForDeletion()
                && selectedObject.BeatmapData is not null)
            {
                sources.Add(selectedObject);
                data.Add(selectedObject.BeatmapData);
            }
        }
        if (data.Count == 0)
        {
            return;
        }

        _objectEditPlane?.ResetBulkSelection();
        CommitBulkSelection();
        _rightDrag = null;
        ClearClipboard();
        _clipboard.Copy(data);
        foreach (var source in sources)
        {
            if (cut)
            {
                _cutObjects.Add(source);
                source.SetCut(true);
            }
            else
            {
                _copiedObjects.Add(source);
                source.SetCopied(true);
            }
        }
        ClipboardChanged?.Invoke(ClipboardCount);
    }

    public void PasteCopiedObjects()
    {
        var beatmap = BeatMapManager.CurrentBeatmap;
        if (beatmap is null || ClipboardCount == 0)
        {
            return;
        }

        var pastedData = new HashSet<BeatMapObjectBase>(_clipboard.CreatePaste(PlaybackManager.PlaybackBeat));
        var cutSources = new List<BeatmapObject>(_cutObjects);
        DeselectAllObjects();
        _rightDrag = null;
        foreach (var data in pastedData)
        {
            beatmap.AddObject(data);
        }

        foreach (var child in GetNode<NoteBlockLane>("NoteBlockLane").GetChildren())
        {
            if (child is BeatmapObject beatmapObject && pastedData.Contains(beatmapObject.BeatmapData))
            {
                AddSelectedObject(beatmapObject);
            }
        }

        ClearCutSources();
        foreach (var source in cutSources)
        {
            if (IsInstanceValid(source) && !source.IsQueuedForDeletion())
            {
                source.DeleteBeatmapObject();
            }
        }

        CopySelectedObjects();
        EmitSelectionChanged();
    }

    private void ClearCopiedSources()
    {
        foreach (var source in _copiedObjects)
        {
            if (IsInstanceValid(source))
            {
                source.SetCopied(false);
            }
        }
        _copiedObjects.Clear();
    }

    private void ClearCutSources()
    {
        foreach (var source in _cutObjects)
        {
            if (IsInstanceValid(source))
            {
                source.SetCut(false);
            }
        }
        _cutObjects.Clear();
    }

    private void ClearClipboard()
    {
        ClearCopiedSources();
        ClearCutSources();
        _clipboard.Clear();
        ClipboardChanged?.Invoke(ClipboardCount);
    }

    public void DeleteSelectedObjects()
    {
        _objectEditPlane?.ResetBulkSelection();
        CommitBulkSelection();
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
        _objectEditPlane?.ResetBulkSelection();
        CommitBulkSelection();
        var selectedObjects = new List<BeatmapObject>(_selectedObjects);
        _selectedObjects.Clear();
        foreach (var selectedObject in selectedObjects)
        {
            selectedObject.SetSelected(false);
        }

        EmitSelectionChanged();
    }

    public void CommitBulkSelection()
    {
        _bulkSelectedObjects.Clear();
    }

    public void UpdateBulkSelection(IEnumerable<BeatmapObject> objects)
    {
        var matches = new HashSet<BeatmapObject>(objects);
        var changed = false;
        foreach (var previous in new List<BeatmapObject>(_bulkSelectedObjects))
        {
            if (!matches.Contains(previous))
            {
                _bulkSelectedObjects.Remove(previous);
                _selectedObjects.Remove(previous);
                previous.SetSelected(false);
                changed = true;
            }
        }

        foreach (var match in matches)
        {
            if (IsInstanceValid(match) && !match.IsQueuedForDeletion() && AddSelectedObject(match))
            {
                _bulkSelectedObjects.Add(match);
                changed = true;
            }
        }

        if (changed)
        {
            EmitSelectionChanged();
        }
    }

    private bool AddSelectedObject(BeatmapObject beatmapObject)
    {
        if (!_selectedObjects.Add(beatmapObject))
        {
            return false;
        }

        beatmapObject.SetSelected(true);
        if (_observedObjects.Add(beatmapObject))
        {
            beatmapObject.TreeExiting += () =>
            {
                _observedObjects.Remove(beatmapObject);
                if (_copiedObjects.Remove(beatmapObject))
                {
                    beatmapObject.SetCopied(false);
                }
                if (_cutObjects.Remove(beatmapObject))
                {
                    beatmapObject.SetCut(false);
                }
                RemoveSelectedObject(beatmapObject);
            };
        }
        return true;
    }

    private void OnBulkSelectionModeChanged(bool enabled)
    {
        if (enabled)
        {
            _leftSelectionMode = false;
            _rightDrag = null;
            _objectEditPlane.SetSelectionModeEnabled(false);
        }
    }

    private void OnCurrentBeatmapChanged(BeatMap beatmap)
    {
        ClearClipboard();
        DeselectAllObjects();
    }

    public override void _ExitTree()
    {
        BeatMapManager.CurrentBeatmapChanged -= OnCurrentBeatmapChanged;
        _objectEditPlane.BulkSelectionModeChanged -= OnBulkSelectionModeChanged;
        InputManager.LeftHand.ButtonPressed -= OnLeftHandButtonPressed;
        InputManager.LeftHand.ButtonReleased -= OnLeftHandButtonReleased;
        InputManager.RightHand.ButtonPressed -= OnRightHandButtonPressed;
        InputManager.RightHand.ButtonReleased -= OnRightHandButtonReleased;
        PlaybackManager.ModeChanged -= OnPlaybackModeChanged;
        ClearClipboard();
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
            AddSelectedObject(hoveredObject);
        }

        EmitSelectionChanged();
    }

    private void RemoveSelectedObject(BeatmapObject beatmapObject)
    {
        _bulkSelectedObjects.Remove(beatmapObject);
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

        SetPointerEditingEnabled(InputManager.LeftHandPointer, editing);
        SetPointerEditingEnabled(InputManager.RightHandPointer, editing);
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
        var hand = saberType == Saber.SaberType.Left ? InputManager.LeftHand : InputManager.RightHand;
        hand.TriggerHapticPulse(Inputs.Haptic, 0.0f, 1.0f, 0.15f, 0.0f);
    }
}