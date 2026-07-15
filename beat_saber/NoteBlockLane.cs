using System;
using Godot;

[GlobalClass]
public partial class NoteBlockLane : Node3D
{
    public const float BeatmapObjectLineSize = 0.5f;
    public const int GridWidth = 4;
    public const int GridHeight = 3;
    public const float LaneWidth = BeatmapObjectLineSize * GridWidth;
    public const float LaneHeight = BeatmapObjectLineSize * GridHeight;

    [Export]
    public PackedScene NoteBlockScene { get; set; }

    [Export]
    public PackedScene BombScene { get; set; }

    [Export]
    public PackedScene WallScene { get; set; }

    [Export]
    public AudioStreamPlayer Music { get; set; }

    public BeatMapDifficultyInfo DifficultyInfo { get; private set; }

    public override void _Ready()
    {
        var manager = GetNode<BeatMapManager>("/root/BeatMapManager");
        OnCurrentBeatmapDifficultyInfoChanged(manager.CurrentBeatmapDifficultyInfo);
        OnCurrentBeatmapChanged(manager.CurrentBeatmap);
        manager.CurrentBeatmapDifficultyInfoChanged += OnCurrentBeatmapDifficultyInfoChanged;
        manager.CurrentBeatmapChanged += OnCurrentBeatmapChanged;
    }

    public void ClearObjects()
    {
        foreach (var child in GetChildren())
        {
            if (child is BeatmapObject beatmapObject)
            {
                beatmapObject.QueueFree();
            }
        }
    }

    public void AddNoteBlock(BeatMapNote note)
    {
        var objectPosition = GetBeatmapObjectInitialPosition(note, DifficultyInfo);
        var noteBlock = NoteBlockScene.Instantiate<NoteBlock>();
        noteBlock.InitializeNote(objectPosition, DifficultyInfo, note);
        AddChild(noteBlock);
    }

    public void AddBomb(BeatMapBomb bomb)
    {
        var objectPosition = GetBeatmapObjectInitialPosition(bomb, DifficultyInfo);
        var bombNode = BombScene.Instantiate<Bomb>();
        bombNode.InitializeBomb(objectPosition, DifficultyInfo, bomb);
        AddChild(bombNode);
    }

    public void AddWall(BeatMapWall wall)
    {
        var objectPosition = GetBeatmapObjectInitialPosition(wall, DifficultyInfo);
        var wallNode = WallScene.Instantiate<Wall>();
        wallNode.InitializeWall(objectPosition, DifficultyInfo, wall);
        AddChild(wallNode);
    }

    private void OnCurrentBeatmapDifficultyInfoChanged(BeatMapDifficultyInfo difficulty)
    {
        DifficultyInfo = difficulty;
    }

    private void OnCurrentBeatmapChanged(BeatMap beatmap)
    {
        ClearObjects();
        if (beatmap is null)
        {
            return;
        }

        foreach (var note in beatmap.Notes)
        {
            AddNoteBlock(note);
        }

        foreach (var bomb in beatmap.Bombs)
        {
            AddBomb(bomb);
        }

        foreach (var wall in beatmap.Walls)
        {
            AddWall(wall);
        }

        beatmap.ObjectAdded += OnObjectAdded;
    }

    private Vector3 GetBeatmapObjectInitialPosition(
        BeatMapObjectBase beatmapObject,
        BeatMapDifficultyInfo mapInfo)
    {
        var hitTime = (float)(beatmapObject.Beat * 60.0 / mapInfo.Bpm);
        var objectPosition = Position + Vector3.Forward * mapInfo.Njs * hitTime;
        var lineIndex = beatmapObject switch
        {
            BeatMapNote note => note.LineIndex,
            BeatMapBomb bomb => bomb.LineIndex,
            BeatMapWall wall => wall.LineIndex,
            _ => throw new ArgumentException("Unknown beatmap object type", nameof(beatmapObject)),
        };
        var lineLayer = beatmapObject switch
        {
            BeatMapNote note => note.LineLayer,
            BeatMapBomb bomb => bomb.LineLayer,
            BeatMapWall wall => wall.LineLayer,
            _ => 0,
        };

        objectPosition += Vector3.Right * BeatmapObjectLineSize * lineIndex;
        objectPosition += Vector3.Up * BeatmapObjectLineSize * lineLayer;
        objectPosition += Vector3.Left * LaneWidth / 2.0f;
        objectPosition += Vector3.Right * BeatmapObjectLineSize / 2.0f;

        objectPosition += Vector3.Up * GlobalSettings.PlayerHeight / 3.0f;
        objectPosition += Vector3.Up * BeatmapObjectLineSize / 2.0f;
        return objectPosition;
    }

    private void OnObjectAdded(BeatMapObjectBase beatmapObject)
    {
        switch (beatmapObject)
        {
            case BeatMapNote note:
                AddNoteBlock(note);
                break;
            case BeatMapBomb bomb:
                AddBomb(bomb);
                break;
            case BeatMapWall wall:
                AddWall(wall);
                break;
            default:
                throw new ArgumentException("Unknown beatmap object type", nameof(beatmapObject));
        }
    }
}