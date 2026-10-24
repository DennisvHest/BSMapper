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
    public event Action CutSelected;
    public event Action PasteCopied;
    public event Action<int> MoveSelectedBySubdivision;

    private Label _title;
    private Control _colorSection;
    private IconButton _copyButton;
    private IconButton _cutButton;
    private IconButton _pasteButton;
    private IconButton _moveBackButton;
    private IconButton _moveForwardButton;

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _colorSection = GetNode<Control>("%ColorSection");
        _copyButton = GetNode<IconButton>("%CopyButton");
        _cutButton = GetNode<IconButton>("%CutButton");
        _pasteButton = GetNode<IconButton>("%PasteButton");
        _moveBackButton = GetNode<IconButton>("%MoveBackButton");
        _moveForwardButton = GetNode<IconButton>("%MoveForwardButton");
        GetNode<Button>("%LeftColorButton").Pressed += () =>
            NoteTypeSelected?.Invoke(BeatMapNote.NoteBlockType.Left);
        GetNode<Button>("%RightColorButton").Pressed += () =>
            NoteTypeSelected?.Invoke(BeatMapNote.NoteBlockType.Right);
        GetNode<Button>("%AnyDirectionButton").Pressed += () =>
            CutDirectionSelected?.Invoke(BeatMapNote.CutDirection.Any);
        GetNode<IconButton>("%DeselectButton").Pressed += () => DeselectAll?.Invoke();
        GetNode<IconButton>("%DeleteButton").Pressed += () => DeleteSelected?.Invoke();
        _copyButton.Pressed += () => CopySelected?.Invoke();
        _cutButton.Pressed += () => CutSelected?.Invoke();
        _pasteButton.Pressed += () => PasteCopied?.Invoke();
        _moveBackButton.Pressed += () => MoveSelectedBySubdivision?.Invoke(-1);
        _moveForwardButton.Pressed += () => MoveSelectedBySubdivision?.Invoke(1);
        SetSelection(0, false);
    }

    public void SetSelection(int selectedCount, bool containsNotes, int clipboardCount = 0)
    {
        _title.Text = $"Selected: {selectedCount}";
        _colorSection.Visible = containsNotes;
        _copyButton.Disabled = selectedCount == 0;
        _cutButton.Disabled = selectedCount == 0;
        _pasteButton.Disabled = clipboardCount == 0;
        _moveBackButton.Disabled = selectedCount == 0;
        _moveForwardButton.Disabled = selectedCount == 0;
        _pasteButton.LabelText = clipboardCount > 0 ? $"({clipboardCount})" : "";
        GetNode<IconButton>("%DeselectButton").Disabled = selectedCount == 0;
        GetNode<IconButton>("%DeleteButton").Disabled = selectedCount == 0;
    }
}
