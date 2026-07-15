using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BeatMapObjectBase : RefCounted
{
    [Export]
    public Variant OriginalObject { get; set; }

    [Export]
    public double Beat { get; set; }

    public virtual void SaveV2Object()
    {
        if (OriginalObject.VariantType != Variant.Type.Dictionary)
        {
            OriginalObject = new Dictionary();
        }

        OriginalObject.AsGodotDictionary()["_time"] = Beat;
    }
}