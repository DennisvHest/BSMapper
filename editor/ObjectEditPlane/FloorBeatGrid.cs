using Godot;

[GlobalClass]
public partial class FloorBeatGrid : Node3D
{
    private const float FloorMarkerY = 0.01f;
    private const float LineHeight = 0.002f;
    private const float MajorLineThickness = 0.088f;
    private const float MinorLineThickness = 0.006f;
    private const float LongitudinalLineThickness = 0.01f;
    private const float CurrentBeatLineThickness = 0.088f;
    private const float VisibleBeatsAhead = 8.0f;
    private const float VisibleBeatsBehind = 2.0f;
    private const float QuarterBeatStep = 0.25f;
    private const float BeatLabelOffsetX = 0.35f;
    private const float BeatLabelHeight = 0.04f;
    private const float BeatLabelPixelSize = 0.005f;

    private Node3D _floorGridRoot;
    private MeshInstance3D _currentBeatMarker;
    private BoxMesh _lineMesh;
    private StandardMaterial3D _majorLineMaterial;
    private StandardMaterial3D _minorLineMaterial;
    private StandardMaterial3D _longitudinalLineMaterial;
    private StandardMaterial3D _currentBeatLineMaterial;
    private int _renderedWindowStartQuarter = -1;
    private int _renderedWindowEndQuarter = -1;

    private PlaybackManager PlaybackManager => GetNode<PlaybackManager>("/root/PlaybackManager");
    private BeatMapManager BeatMapManager => GetNode<BeatMapManager>("/root/BeatMapManager");

    public override void _Ready()
    {
        InitializeFloorGrid();
        OnPlaybackModeChanged();
        RebuildFloorGrid();
        PlaybackManager.ModeChanged += OnPlaybackModeChanged;
        BeatMapManager.CurrentBeatmapChanged += OnCurrentBeatmapChanged;
        BeatMapManager.CurrentBeatmapDifficultyInfoChanged += OnCurrentBeatmapDifficultyInfoChanged;
        UpdateFloorGridPosition();
    }

    public override void _Process(double delta)
    {
        RefreshVisibleWindowIfNeeded();
        UpdateFloorGridPosition();
    }

    private void InitializeFloorGrid()
    {
        _lineMesh = new BoxMesh();
        _majorLineMaterial = CreateLineMaterial(new Color(1.0f, 1.0f, 1.0f, 0.32f));
        _minorLineMaterial = CreateLineMaterial(new Color(1.0f, 1.0f, 1.0f, 0.12f));
        _longitudinalLineMaterial = CreateLineMaterial(new Color(1.0f, 1.0f, 1.0f, 0.18f));
        _currentBeatLineMaterial = CreateLineMaterial(new Color(0.3f, 0.8f, 1.0f, 0.75f));

        _floorGridRoot = new Node3D { Name = "FloorBeatGrid" };
        AddChild(_floorGridRoot);
        _currentBeatMarker = CreateLine(
            "CurrentBeatMarker",
            new Vector3(NoteBlockLane.LaneWidth, LineHeight, CurrentBeatLineThickness),
            _currentBeatLineMaterial);
        _currentBeatMarker.Position = new Vector3(0.0f, FloorMarkerY, 0.0f);
        AddChild(_currentBeatMarker);
    }

    private static StandardMaterial3D CreateLineMaterial(Color color)
    {
        return new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = color,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
    }

    private MeshInstance3D CreateLine(string lineName, Vector3 lineScale, Material material)
    {
        return new MeshInstance3D
        {
            Name = lineName,
            Mesh = _lineMesh,
            MaterialOverride = material,
            Scale = lineScale,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static Label3D CreateBeatLabel(int beatNumber, float beatPosition)
    {
        return new Label3D
        {
            Name = $"BeatLabel{beatNumber}",
            Text = beatNumber.ToString(),
            FontSize = 64,
            PixelSize = BeatLabelPixelSize,
            Modulate = new Color(1.0f, 1.0f, 1.0f, 0.85f),
            OutlineModulate = new Color(0.0f, 0.0f, 0.0f, 0.65f),
            OutlineSize = 8,
            NoDepthTest = true,
            Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
            Position = new Vector3(
                NoteBlockLane.LaneWidth / 2.0f + BeatLabelOffsetX,
                BeatLabelHeight,
                -beatPosition),
            Rotation = new Vector3(Mathf.DegToRad(-90.0f), 0.0f, 0.0f),
        };
    }

    private void RebuildFloorGrid()
    {
        _renderedWindowStartQuarter = -1;
        _renderedWindowEndQuarter = -1;
        RefreshVisibleWindowIfNeeded();
    }

    private void RefreshVisibleWindowIfNeeded()
    {
        var difficulty = BeatMapManager.CurrentBeatmapDifficultyInfo;
        if (difficulty is null || difficulty.BeatDuration == 0.0f)
        {
            ClearFloorLines();
            _currentBeatMarker.Hide();
            return;
        }

        _currentBeatMarker.Show();
        var totalBeats = GetTotalBeats(difficulty);
        if (totalBeats <= 0.0f)
        {
            ClearFloorLines();
            return;
        }

        var visibleWindow = GetVisibleWindow(totalBeats);
        var windowStartQuarter = Mathf.RoundToInt(visibleWindow.X / QuarterBeatStep);
        var windowEndQuarter = Mathf.RoundToInt(visibleWindow.Y / QuarterBeatStep);
        if (windowStartQuarter == _renderedWindowStartQuarter
            && windowEndQuarter == _renderedWindowEndQuarter)
        {
            return;
        }

        _renderedWindowStartQuarter = windowStartQuarter;
        _renderedWindowEndQuarter = windowEndQuarter;
        ClearFloorLines();
        var beatLengthMeters = difficulty.Njs * difficulty.BeatDuration;
        SpawnLongitudinalLines(visibleWindow.X, visibleWindow.Y, beatLengthMeters);
        SpawnCrossLines(windowStartQuarter, windowEndQuarter, beatLengthMeters);
        UpdateFloorGridPosition();
    }

    private void ClearFloorLines()
    {
        foreach (var child in _floorGridRoot.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void SpawnLongitudinalLines(float startBeat, float endBeat, float beatLengthMeters)
    {
        var centerBeat = (startBeat + endBeat) / 2.0f;
        var lengthMeters = (endBeat - startBeat) * beatLengthMeters;
        for (var lineIndex = 0; lineIndex <= NoteBlockLane.GridWidth; lineIndex++)
        {
            var xPosition = -NoteBlockLane.LaneWidth / 2.0f
                + NoteBlockLane.BeatmapObjectLineSize * lineIndex;
            var line = CreateLine(
                $"LaneBoundary{lineIndex}",
                new Vector3(LongitudinalLineThickness, LineHeight, lengthMeters),
                _longitudinalLineMaterial);
            line.Position = new Vector3(xPosition, FloorMarkerY, -centerBeat * beatLengthMeters);
            _floorGridRoot.AddChild(line);
        }
    }

    private void SpawnCrossLines(int startQuarter, int endQuarter, float beatLengthMeters)
    {
        for (var quarterIndex = startQuarter; quarterIndex <= endQuarter; quarterIndex++)
        {
            var isMajorLine = quarterIndex % 4 == 0;
            var thickness = isMajorLine ? MajorLineThickness : MinorLineThickness;
            var material = isMajorLine ? _majorLineMaterial : _minorLineMaterial;
            var beatPosition = quarterIndex * QuarterBeatStep;
            var line = CreateLine(
                $"BeatMarker{quarterIndex}",
                new Vector3(NoteBlockLane.LaneWidth, LineHeight, thickness),
                material);
            line.Position = new Vector3(0.0f, FloorMarkerY, -beatPosition * beatLengthMeters);
            _floorGridRoot.AddChild(line);

            if (isMajorLine)
            {
                _floorGridRoot.AddChild(CreateBeatLabel(
                    Mathf.RoundToInt(beatPosition),
                    beatPosition * beatLengthMeters));
            }
        }
    }

    private void UpdateFloorGridPosition()
    {
        var difficulty = BeatMapManager.CurrentBeatmapDifficultyInfo;
        if (difficulty is not null)
        {
            _floorGridRoot.Position = new Vector3(
                0.0f,
                0.0f,
                (float)PlaybackManager.PlaybackPosition * difficulty.Njs);
        }
    }

    private float GetTotalBeats(BeatMapDifficultyInfo difficulty)
    {
        var lastBeat = GetMusicTotalBeats(difficulty);
        var beatmap = BeatMapManager.CurrentBeatmap;
        if (beatmap is not null)
        {
            foreach (var note in beatmap.Notes)
            {
                lastBeat = Mathf.Max(lastBeat, (float)note.Beat);
            }

            foreach (var bomb in beatmap.Bombs)
            {
                lastBeat = Mathf.Max(lastBeat, (float)bomb.Beat);
            }

            foreach (var wall in beatmap.Walls)
            {
                lastBeat = Mathf.Max(lastBeat, (float)(wall.Beat + wall.Duration));
            }
        }

        return Mathf.Ceil(lastBeat);
    }

    private float GetMusicTotalBeats(BeatMapDifficultyInfo difficulty)
    {
        return PlaybackManager.Music?.Stream is null
            ? 0.0f
            : (float)(PlaybackManager.Music.Stream.GetLength() / difficulty.BeatDuration);
    }

    private Vector2 GetVisibleWindow(float totalBeats)
    {
        var currentBeat = (float)PlaybackManager.PlaybackBeat;
        var startBeat = Mathf.Max(
            Mathf.Floor((currentBeat - VisibleBeatsBehind) / QuarterBeatStep) * QuarterBeatStep,
            0.0f);
        var endBeat = Mathf.Min(
            Mathf.Ceil((currentBeat + VisibleBeatsAhead) / QuarterBeatStep) * QuarterBeatStep,
            totalBeats);
        return new Vector2(startBeat, endBeat);
    }

    private void OnCurrentBeatmapChanged(BeatMap beatmap)
    {
        RebuildFloorGrid();
    }

    private void OnCurrentBeatmapDifficultyInfoChanged(BeatMapDifficultyInfo difficulty)
    {
        RebuildFloorGrid();
    }

    private void OnPlaybackModeChanged()
    {
        Visible = PlaybackManager.Mode == PlaybackManager.EditMode.Editing;
    }
}