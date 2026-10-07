using System;
using Godot;

[GlobalClass]
public partial class BeatSubdivisionSelectorUI : Control
{
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
            button.SetPressedNoSignal(subdivision == selectedSubdivision);
        }
    }
}
