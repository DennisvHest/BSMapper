using Godot;

[GlobalClass]
public partial class ObjectTypeSelectorUI : Control
{
    [Signal]
    public delegate void PlaceableSelectedEventHandler(ObjectEditPlane.PlaceableObjectType selectedObjectType);

    [Signal]
    public delegate void BulkSelectionToggledEventHandler();

    [Export]
    public StyleBox IdleButtonStyle { get; set; }

    [Export]
    public StyleBox HoverButtonStyle { get; set; }

    [Export]
    public StyleBox SelectedButtonStyle { get; set; }

    [Export]
    public Color IdleTextColor { get; set; } = new(0.83f, 0.86f, 0.9f);

    [Export]
    public Color SelectedTextColor { get; set; } = Colors.White;

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
        ApplyButtonState(_noteButton,
            !_bulkSelectionEnabled && _selectedObjectType == ObjectEditPlane.PlaceableObjectType.NoteBlock);
        ApplyButtonState(
            _anyDirectionNoteButton,
            !_bulkSelectionEnabled && _selectedObjectType == ObjectEditPlane.PlaceableObjectType.AnyDirectionNoteBlock);
        ApplyButtonState(_bombButton,
            !_bulkSelectionEnabled && _selectedObjectType == ObjectEditPlane.PlaceableObjectType.Bomb);
        ApplyButtonState(_bulkSelectionButton, _bulkSelectionEnabled);
        _bulkSelectionButton.Text = _bulkSelectionEnabled ? "Bulk select: ON" : "Bulk select: OFF";
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

    private void ApplyButtonState(Button button, bool isSelected)
    {
        var normalStyle = isSelected ? SelectedButtonStyle : IdleButtonStyle;
        var activeHoverStyle = isSelected ? SelectedButtonStyle : HoverButtonStyle;
        var textColor = isSelected ? SelectedTextColor : IdleTextColor;
        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeStyleboxOverride("hover", activeHoverStyle);
        button.AddThemeStyleboxOverride("pressed", SelectedButtonStyle);
        button.AddThemeStyleboxOverride("focus", activeHoverStyle);
        button.AddThemeStyleboxOverride("disabled", normalStyle);
        button.AddThemeColorOverride("font_color", textColor);
        button.AddThemeColorOverride("font_hover_color", textColor);
        button.AddThemeColorOverride("font_pressed_color", SelectedTextColor);
        button.AddThemeColorOverride("font_focus_color", textColor);
    }
}