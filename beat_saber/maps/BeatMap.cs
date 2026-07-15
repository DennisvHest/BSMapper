using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

[GlobalClass]
public partial class BeatMap : RefCounted
{
    [Signal]
    public delegate void ObjectAddedEventHandler(BeatMapObjectBase @object);

    [Signal]
    public delegate void ObjectRemovedEventHandler(BeatMapObjectBase @object);

    [Export]
    public Variant OriginalMap { get; set; }

    [Export]
    public Array<BeatMapNote> Notes { get; set; } = new();

    [Export]
    public Array<BeatMapBomb> Bombs { get; set; } = new();

    [Export]
    public Array<BeatMapWall> Walls { get; set; } = new();

    public void InitializeEmpty()
    {
        OriginalMap = new Dictionary
        {
            ["_version"] = "2.0.0",
            ["_notes"] = new Array(),
            ["_obstacles"] = new Array(),
            ["_events"] = new Array(),
            ["_customData"] = new Dictionary(),
        };
    }

    public void LoadFromFile(Variant map)
    {
        var mapDictionary = map.AsGodotDictionary();
        var version = mapDictionary["_version"].AsString();
        if (!version.StartsWith('2'))
        {
            throw new ArgumentException("Map version is not supported", nameof(map));
        }

        OriginalMap = map;
        Notes.Clear();
        Bombs.Clear();
        Walls.Clear();

        foreach (var objectVariant in mapDictionary["_notes"].AsGodotArray())
        {
            var objectDictionary = objectVariant.AsGodotDictionary();
            if (objectDictionary["_type"].AsDouble() == BeatMapBomb.BombType)
            {
                Bombs.Add(BeatMapBomb.FromV2Object(objectVariant));
            }
            else
            {
                Notes.Add(BeatMapNote.FromV2Object(objectVariant));
            }
        }

        foreach (var wallVariant in mapDictionary["_obstacles"].AsGodotArray())
        {
            Walls.Add(BeatMapWall.FromV2Object(wallVariant));
        }
    }

    public void AddObject(BeatMapObjectBase @object)
    {
        switch (@object)
        {
            case BeatMapNote note:
                Notes.Add(note);
                break;
            case BeatMapBomb bomb:
                Bombs.Add(bomb);
                break;
            case BeatMapWall wall:
                Walls.Add(wall);
                break;
            default:
                throw new ArgumentException("Unknown beatmap object type", nameof(@object));
        }

        EmitSignal(SignalName.ObjectAdded, @object);
    }

    public void RemoveObject(BeatMapObjectBase @object)
    {
        var removed = @object switch
        {
            BeatMapNote note => Notes.Remove(note),
            BeatMapBomb bomb => Bombs.Remove(bomb),
            BeatMapWall wall => Walls.Remove(wall),
            _ => throw new ArgumentException("Unknown beatmap object type", nameof(@object)),
        };

        if (removed)
        {
            EmitSignal(SignalName.ObjectRemoved, @object);
        }
    }

    public void SaveChanges()
    {
        var map = OriginalMap.AsGodotDictionary();
        var originalNotes = map["_notes"].AsGodotArray();
        var originalWalls = map["_obstacles"].AsGodotArray();
        originalNotes.Clear();
        originalWalls.Clear();

        var allNotes = SaveObjects(Notes);
        allNotes.AddRange(SaveObjects(Bombs));
        allNotes.Sort(CompareObjectTimes);
        foreach (var note in allNotes)
        {
            originalNotes.Add(note);
        }

        var allWalls = SaveObjects(Walls);
        allWalls.Sort(CompareObjectTimes);
        foreach (var wall in allWalls)
        {
            originalWalls.Add(wall);
        }
    }

    private static List<Variant> SaveObjects<[MustBeVariant] T>(Array<T> objects) where T : BeatMapObjectBase
    {
        var savedObjects = new List<Variant>(objects.Count);
        foreach (var @object in objects)
        {
            @object.SaveV2Object();
            savedObjects.Add(@object.OriginalObject);
        }

        return savedObjects;
    }

    private static int CompareObjectTimes(Variant left, Variant right)
    {
        var leftTime = left.AsGodotDictionary()["_time"].AsDouble();
        var rightTime = right.AsGodotDictionary()["_time"].AsDouble();
        return leftTime.CompareTo(rightTime);
    }
}