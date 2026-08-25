using System;
using Godot;

[GlobalClass]
public partial class BeatSubdivisionSelectorUI : Control
{
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

    public event Action<int> SubdivisionSelected;

    private readonly (string Name, int Subdivision)[] _buttonDefinitions =
    {
        ("BeatSubdivision1Button", 1),
        ("BeatSubdivision2Button", 2),
        ("BeatSubdivision4Button", 4),
        ("BeatSubdivision8Button", 8),
        ("BeatSubdivision16Button", 16),
    };

    public override void _Ready()
    {
        foreach (var (buttonName, subdivision) in _buttonDefinitions)
        {
            GetNode<Button>($"%{buttonName}").Pressed += () => SubdivisionSelected?.Invoke(subdivision);
        }
    }

    public void SetSelectedSubdivision(int selectedSubdivision)
    {
        foreach (var (buttonName, subdivision) in _buttonDefinitions)
        {
            var button = GetNode<Button>($"%{buttonName}");
            var isSelected = subdivision == selectedSubdivision;
            button.ButtonPressed = isSelected;
            ApplyButtonState(button, isSelected);
        }
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
