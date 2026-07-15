using Godot;

[GlobalClass]
public partial class BeatMapWall : BeatMapObjectBase
{
    public enum WallType
    {
        Full,
        Crouch,
        Free,
    }

    public const double FullWallHeight = 5.0;
    public const double CrouchWallHeight = 3.0;

    [Export]
    public int LineIndex { get; set; }

    [Export]
    public int LineLayer { get; set; }

    [Export]
    public WallType Type { get; set; }

    [Export]
    public double Duration { get; set; }

    [Export]
    public int Width { get; set; }

    [Export]
    public int Height { get; set; }

    public static BeatMapWall FromV2Object(Variant original)
    {
        var data = original.AsGodotDictionary();
        var type = (WallType)data["_type"].AsInt32();
        return new BeatMapWall
        {
            OriginalObject = original,
            Beat = data["_time"].AsDouble(),
            LineIndex = data["_lineIndex"].AsInt32(),
            LineLayer = data.ContainsKey("_lineLayer") ? data["_lineLayer"].AsInt32() : 0,
            Type = type,
            Duration = data["_duration"].AsDouble(),
            Width = data["_width"].AsInt32(),
            Height = type switch
            {
                WallType.Full => (int)FullWallHeight,
                WallType.Crouch => (int)CrouchWallHeight,
                _ => data.ContainsKey("_height") ? data["_height"].AsInt32() : 0,
            },
        };
    }

    public override void SaveV2Object()
    {
        base.SaveV2Object();
        var data = OriginalObject.AsGodotDictionary();
        data["_type"] = (int)Type;
        data["_duration"] = Duration;
        data["_lineIndex"] = LineIndex;
        data["_width"] = Width;
    }
}