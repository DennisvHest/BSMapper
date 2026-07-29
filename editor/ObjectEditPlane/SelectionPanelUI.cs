using System;
using Godot;

[GlobalClass]
public partial class SelectionPanelUI : Control
{
    public event Action<BeatMapNote.NoteBlockType> NoteTypeSelected;
    public event Action DeleteSelected;

    private Label _title;
    private Control _colorSection;

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _colorSection = GetNode<Control>("%ColorSection");
        GetNode<Button>("%LeftColorButton").Pressed += () =>
            NoteTypeSelected?.Invoke(BeatMapNote.NoteBlockType.Left);
        GetNode<Button>("%RightColorButton").Pressed += () =>
            NoteTypeSelected?.Invoke(BeatMapNote.NoteBlockType.Right);
        GetNode<Button>("%DeleteButton").Pressed += () => DeleteSelected?.Invoke();
    }

    public void SetSelection(int selectedCount, bool containsNotes)
    {
        _title.Text = $"Selected: {selectedCount}";
        _colorSection.Visible = containsNotes;
    }
}
