using Godot;

[GlobalClass]
public partial class BeatMapBomb : BeatMapObjectBase
{
    public const double BombType = 3.0;

    [Export]
    public int LineIndex { get; set; }

    [Export]
    public int LineLayer { get; set; }

    public static BeatMapBomb FromV2Object(Variant original)
    {
        var data = original.AsGodotDictionary();
        return new BeatMapBomb
        {
            OriginalObject = original,
            Beat = data["_time"].AsDouble(),
            LineIndex = data["_lineIndex"].AsInt32(),
            LineLayer = data["_lineLayer"].AsInt32(),
        };
    }

    public override void SaveV2Object()
    {
        base.SaveV2Object();
        var data = OriginalObject.AsGodotDictionary();
        data["_type"] = BombType;
        data["_lineIndex"] = LineIndex;
        data["_lineLayer"] = LineLayer;
        data["_cutDirection"] = 8;
    }
}