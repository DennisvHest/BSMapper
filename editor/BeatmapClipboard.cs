using System;
using System.Collections.Generic;
using Godot;

public sealed class BeatmapClipboard
{
    private readonly List<BeatMapObjectBase> _objects = new();
    private double _firstBeat;

    public int Count => _objects.Count;

    public void Copy(IEnumerable<BeatMapObjectBase> objects)
    {
        var snapshots = new List<BeatMapObjectBase>();
        var firstBeat = double.PositiveInfinity;
        foreach (var data in objects)
        {
            snapshots.Add(Clone(data, data.Beat));
            firstBeat = Math.Min(firstBeat, data.Beat);
        }

        _objects.Clear();
        _objects.AddRange(snapshots);
        _firstBeat = snapshots.Count > 0 ? firstBeat : 0.0;
    }

    public IReadOnlyList<BeatMapObjectBase> CreatePaste(double playbackBeat)
    {
        var result = new List<BeatMapObjectBase>(_objects.Count);
        foreach (var data in _objects)
        {
            result.Add(Clone(data, playbackBeat + (data.Beat - _firstBeat)));
        }
        return result;
    }

    public void Clear()
    {
        _objects.Clear();
        _firstBeat = 0.0;
    }

    private static BeatMapObjectBase Clone(BeatMapObjectBase data, double beat)
    {
        BeatMapObjectBase copy = data switch
        {
            BeatMapNote note => new BeatMapNote
            {
                LineIndex = note.LineIndex,
                LineLayer = note.LineLayer,
                Type = note.Type,
                Cut = note.Cut,
            },
            BeatMapBomb bomb => new BeatMapBomb
            {
                LineIndex = bomb.LineIndex,
                LineLayer = bomb.LineLayer,
            },
            BeatMapWall wall => new BeatMapWall
            {
                LineIndex = wall.LineIndex,
                LineLayer = wall.LineLayer,
                Type = wall.Type,
                Width = wall.Width,
                Height = wall.Height,
                Duration = wall.Duration,
            },
            _ => throw new ArgumentException("Unknown beatmap object type", nameof(data)),
        };
        copy.Beat = beat;
        if (data.OriginalObject.VariantType == Variant.Type.Dictionary)
        {
            copy.OriginalObject = data.OriginalObject.AsGodotDictionary().Duplicate(true);
        }
        return copy;
    }
}
