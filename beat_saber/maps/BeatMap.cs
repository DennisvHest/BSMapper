using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

[GlobalClass]
public partial class BeatMap : RefCounted
{
    private const string NoteScriptPath = "res://beat_saber/maps/beat_map_note.gd";
    private const string BombScriptPath = "res://beat_saber/maps/beat_map_bomb.gd";
    private const string WallScriptPath = "res://beat_saber/maps/beat_map_wall.gd";
    private const double BombType = 3.0;

    [Signal]
    public delegate void ObjectAddedEventHandler(GodotObject @object);

    [Signal]
    public delegate void ObjectRemovedEventHandler(GodotObject @object);

    [Export]
    public Variant OriginalMap { get; set; }

    [Export]
    public Array Notes { get; set; } = new();

    [Export]
    public Array Bombs { get; set; } = new();

    [Export]
    public Array Walls { get; set; } = new();

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
            if (objectDictionary["_type"].AsDouble() == BombType)
            {
                Bombs.Add(CreateBomb(objectVariant, objectDictionary));
            }
            else
            {
                Notes.Add(CreateNote(objectVariant, objectDictionary));
            }
        }

        foreach (var wallVariant in mapDictionary["_obstacles"].AsGodotArray())
        {
            Walls.Add(CreateWall(wallVariant, wallVariant.AsGodotDictionary()));
        }
    }

    public void AddObject(GodotObject @object)
    {
        var target = GetObjectArray(@object);
        target.Add(@object);
        EmitSignal(SignalName.ObjectAdded, @object);
    }

    public void RemoveObject(GodotObject @object)
    {
        var target = GetObjectArray(@object);
        var index = target.IndexOf(@object);
        if (index < 0)
        {
            return;
        }

        target.RemoveAt(index);
        EmitSignal(SignalName.ObjectRemoved, @object);
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

    private static GodotObject CreateNote(Variant original, Dictionary data)
    {
        var note = CreateScriptObject(NoteScriptPath);
        note.Set("original_object", original);
        note.Set("beat", data["_time"]);
        note.Set("line_index", data["_lineIndex"]);
        note.Set("line_layer", data["_lineLayer"]);
        note.Set("type", data["_type"].AsInt32());
        note.Set("cut_direction", data["_cutDirection"].AsInt32());
        return note;
    }

    private static GodotObject CreateBomb(Variant original, Dictionary data)
    {
        var bomb = CreateScriptObject(BombScriptPath);
        bomb.Set("original_object", original);
        bomb.Set("beat", data["_time"]);
        bomb.Set("line_index", data["_lineIndex"]);
        bomb.Set("line_layer", data["_lineLayer"]);
        return bomb;
    }

    private static GodotObject CreateWall(Variant original, Dictionary data)
    {
        var wall = CreateScriptObject(WallScriptPath);
        var wallType = data["_type"].AsInt32();
        wall.Set("original_object", original);
        wall.Set("beat", data["_time"]);
        wall.Set("line_index", data["_lineIndex"]);
        wall.Set("line_layer", data.ContainsKey("_lineLayer") ? data["_lineLayer"] : 0);
        wall.Set("type", wallType);
        wall.Set("duration", data["_duration"]);
        wall.Set("width", data["_width"]);
        wall.Set("height", wallType switch
        {
            0 => 5.0,
            1 => 3.0,
            _ => data.ContainsKey("_height") ? data["_height"] : 0,
        });
        return wall;
    }

    private static GodotObject CreateScriptObject(string path)
    {
        var script = GD.Load<Script>(path);
        return script.Call("new").AsGodotObject();
    }

    private Array GetObjectArray(GodotObject @object)
    {
        var scriptPath = @object.GetScript().As<Script>()?.ResourcePath ?? string.Empty;
        return scriptPath switch
        {
            NoteScriptPath => Notes,
            BombScriptPath => Bombs,
            WallScriptPath => Walls,
            _ => throw new ArgumentException("Unknown beatmap object type", nameof(@object)),
        };
    }

    private List<Variant> SaveObjects(Array objects)
    {
        var savedObjects = new List<Variant>(objects.Count);
        foreach (var objectVariant in objects)
        {
            var @object = objectVariant.AsGodotObject();
            @object.Call("save_v2_object");
            savedObjects.Add(@object.Get("original_object"));
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