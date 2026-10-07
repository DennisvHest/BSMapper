using Godot;

[GlobalClass]
public partial class ObjectTypeSelectorUI : Control
{
    [Signal]
    public delegate void PlaceableSelectedEventHandler(ObjectEditPlane.PlaceableObjectType selectedObjectType);

    [Signal]
    public delegate void BulkSelectionToggledEventHandler();

    private Button _noteButton;
    private Button _anyDirectionNoteButton;
    private Button _bombButton;
    private Button _bulkSelectionButton;
    private ObjectEditPlane.PlaceableObjectType _selectedObjectType;
    private bool _bulkSelectionEnabled;

    public override void _Ready()
    {
        _noteButton = GetNode<Button>("%NoteButton");
        _anyDirectionNoteButton = GetNode<Button>("%AnyDirectionNoteButton");
        _bombButton = GetNode<Button>("%BombButton");
        _bulkSelectionButton = GetNode<Button>("%BulkSelectionButton");
        _noteButton.Pressed += OnNoteButtonPressed;
        _anyDirectionNoteButton.Pressed += OnAnyDirectionNoteButtonPressed;
        _bombButton.Pressed += OnBombButtonPressed;
        _bulkSelectionButton.Pressed += () => EmitSignal(SignalName.BulkSelectionToggled);
        SetSelectedObjectType(ObjectEditPlane.PlaceableObjectType.NoteBlock);
    }

    public void SetSelectedObjectType(ObjectEditPlane.PlaceableObjectType selectedObjectType)
    {
        _selectedObjectType = selectedObjectType;
        UpdateButtonStates();
    }

    public void SetBulkSelectionEnabled(bool enabled)
    {
        _bulkSelectionEnabled = enabled;
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        _noteButton.SetPressedNoSignal(
            !_bulkSelectionEnabled && _selectedObjectType == ObjectEditPlane.PlaceableObjectType.NoteBlock);
        _anyDirectionNoteButton.SetPressedNoSignal(
            !_bulkSelectionEnabled && _selectedObjectType == ObjectEditPlane.PlaceableObjectType.AnyDirectionNoteBlock);
        _bombButton.SetPressedNoSignal(
            !_bulkSelectionEnabled && _selectedObjectType == ObjectEditPlane.PlaceableObjectType.Bomb);
        _bulkSelectionButton.SetPressedNoSignal(_bulkSelectionEnabled);
    }

    private void OnNoteButtonPressed()
    {
        EmitSignal(SignalName.PlaceableSelected, (int)ObjectEditPlane.PlaceableObjectType.NoteBlock);
    }

    private void OnBombButtonPressed()
    {
        EmitSignal(SignalName.PlaceableSelected, (int)ObjectEditPlane.PlaceableObjectType.Bomb);
    }

    private void OnAnyDirectionNoteButtonPressed()
    {
        EmitSignal(SignalName.PlaceableSelected, (int)ObjectEditPlane.PlaceableObjectType.AnyDirectionNoteBlock);
    }
}