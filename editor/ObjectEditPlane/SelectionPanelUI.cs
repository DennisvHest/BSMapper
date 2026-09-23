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
    private Button _copyButton;
    private Button _cutButton;
    private Button _pasteButton;
    private Button _moveBackButton;
    private Button _moveForwardButton;

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _colorSection = GetNode<Control>("%ColorSection");
        _copyButton = GetNode<Button>("%CopyButton");
        _cutButton = GetNode<Button>("%CutButton");
        _pasteButton = GetNode<Button>("%PasteButton");
        _moveBackButton = GetNode<Button>("%MoveBackButton");
        _moveForwardButton = GetNode<Button>("%MoveForwardButton");
        GetNode<Button>("%LeftColorButton").Pressed += () =>
            NoteTypeSelected?.Invoke(BeatMapNote.NoteBlockType.Left);
        GetNode<Button>("%RightColorButton").Pressed += () =>
            NoteTypeSelected?.Invoke(BeatMapNote.NoteBlockType.Right);
        GetNode<Button>("%AnyDirectionButton").Pressed += () =>
            CutDirectionSelected?.Invoke(BeatMapNote.CutDirection.Any);
        GetNode<Button>("%DeselectButton").Pressed += () => DeselectAll?.Invoke();
        GetNode<Button>("%DeleteButton").Pressed += () => DeleteSelected?.Invoke();
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
        _pasteButton.Text = clipboardCount > 0 ? $"Paste ({clipboardCount})" : "Paste";
        GetNode<Button>("%DeselectButton").Disabled = selectedCount == 0;
        GetNode<Button>("%DeleteButton").Disabled = selectedCount == 0;
    }
}
