using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BeatMapNote : BeatMapObjectBase
{
    public enum NoteBlockType
    {
        Left,
        Right,
    }

    public enum CutDirection
    {
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight,
        Any,
    }

    [Export]
    public int LineIndex { get; set; }

    [Export]
    public int LineLayer { get; set; }

    [Export]
    public NoteBlockType Type { get; set; }

    [Export]
    public CutDirection Cut { get; set; }

    public static BeatMapNote FromV2Object(Variant original)
    {
        var data = original.AsGodotDictionary();
        return new BeatMapNote
        {
            OriginalObject = original,
            Beat = data["_time"].AsDouble(),
            LineIndex = data["_lineIndex"].AsInt32(),
            LineLayer = data["_lineLayer"].AsInt32(),
            Type = (NoteBlockType)data["_type"].AsInt32(),
            Cut = (CutDirection)data["_cutDirection"].AsInt32(),
        };
    }

    public override void SaveV2Object()
    {
        base.SaveV2Object();
        var data = OriginalObject.AsGodotDictionary();
        data["_type"] = (int)Type;
        data["_cutDirection"] = (int)Cut;
        data["_lineIndex"] = LineIndex;
        data["_lineLayer"] = LineLayer;
    }
}