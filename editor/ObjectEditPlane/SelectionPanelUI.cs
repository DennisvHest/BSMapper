using System;
using Godot;

[GlobalClass]
public partial class SelectionPanelUI : Control
{
    public event Action<BeatMapNote.NoteBlockType> NoteTypeSelected;
    public event Action<BeatMapNote.CutDirection> CutDirectionSelected;
    public event Action DeselectAll;
    public event Action DeleteSelected;
    public event Action CopySelected;
    public event Action PasteCopied;

    private Label _title;
    private Control _colorSection;
    private Button _copyButton;
    private Button _pasteButton;

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _colorSection = GetNode<Control>("%ColorSection");
        _copyButton = GetNode<Button>("%CopyButton");
        _pasteButton = GetNode<Button>("%PasteButton");
        GetNode<Button>("%LeftColorButton").Pressed += () =>
            NoteTypeSelected?.Invoke(BeatMapNote.NoteBlockType.Left);
        GetNode<Button>("%RightColorButton").Pressed += () =>
            NoteTypeSelected?.Invoke(BeatMapNote.NoteBlockType.Right);
        GetNode<Button>("%AnyDirectionButton").Pressed += () =>
            CutDirectionSelected?.Invoke(BeatMapNote.CutDirection.Any);
        GetNode<Button>("%DeselectButton").Pressed += () => DeselectAll?.Invoke();
        GetNode<Button>("%DeleteButton").Pressed += () => DeleteSelected?.Invoke();
        _copyButton.Pressed += () => CopySelected?.Invoke();
        _pasteButton.Pressed += () => PasteCopied?.Invoke();
        SetSelection(0, false);
    }

    public void SetSelection(int selectedCount, bool containsNotes, int clipboardCount = 0)
    {
        _title.Text = $"Selected: {selectedCount}";
        _colorSection.Visible = containsNotes;
        _copyButton.Disabled = selectedCount == 0;
        _pasteButton.Disabled = clipboardCount == 0;
        _pasteButton.Text = clipboardCount > 0 ? $"Paste ({clipboardCount})" : "Paste";
        GetNode<Button>("%DeselectButton").Disabled = selectedCount == 0;
        GetNode<Button>("%DeleteButton").Disabled = selectedCount == 0;
    }
}
