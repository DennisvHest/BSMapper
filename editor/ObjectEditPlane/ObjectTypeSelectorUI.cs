using Godot;

[GlobalClass]
public partial class ObjectTypeSelectorUI : Control
{
    [Signal]
    public delegate void PlaceableSelectedEventHandler(ObjectEditPlane.PlaceableObjectType selectedObjectType);

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
    private Button _bombButton;

    public override void _Ready()
    {
        _noteButton = GetNode<Button>("%NoteButton");
        _bombButton = GetNode<Button>("%BombButton");
        _noteButton.Pressed += OnNoteButtonPressed;
        _bombButton.Pressed += OnBombButtonPressed;
        SetSelectedObjectType(ObjectEditPlane.PlaceableObjectType.NoteBlock);
    }

    public void SetSelectedObjectType(ObjectEditPlane.PlaceableObjectType selectedObjectType)
    {
        ApplyButtonState(_noteButton, selectedObjectType == ObjectEditPlane.PlaceableObjectType.NoteBlock);
        ApplyButtonState(_bombButton, selectedObjectType == ObjectEditPlane.PlaceableObjectType.Bomb);
    }

    private void OnNoteButtonPressed()
    {
        EmitSignal(SignalName.PlaceableSelected, (int)ObjectEditPlane.PlaceableObjectType.NoteBlock);
    }

    private void OnBombButtonPressed()
    {
        EmitSignal(SignalName.PlaceableSelected, (int)ObjectEditPlane.PlaceableObjectType.Bomb);
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