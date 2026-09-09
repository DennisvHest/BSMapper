using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class FloorSpectrogram : Node3D
{
    private const float StripWidth = 1.2f;
    private const float GridGap = 0.15f;
    private const int MaximumCachedTiles = 8;
    private readonly Dictionary<int, TextureTile> _tiles = new();
    private Node3D _strip;
    private MeshInstance3D _currentTimeMarker;
    private ArrayMesh _mesh;
    private BoxMesh _markerMesh;
    private StandardMaterial3D _markerMaterial;
    private BeatMapManager _beatMapManager;
    private CancellationTokenSource _analysisCancellation;
    private Task<AnalysisResult> _analysisTask;
    private string _songPath;
    private double _sampleRate;
    private double _songDuration;
    private int _pendingTile = -1;
    private int _firstVisibleTile = -1;
    private int _lastVisibleTile = -1;
    private int _firstRequiredTile = -1;
    private int _lastRequiredTile = -1;
    private bool _analysisFailed;

    private sealed record AnalysisResult(byte[] Pixels, int TileIndex, double Duration, string Error = null);
    private sealed record TextureTile(MeshInstance3D Mesh, StandardMaterial3D Material, ImageTexture Texture);

    public override void _Ready()
    {
        Position = new Vector3(-NoteBlockLane.LaneWidth / 2.0f - GridGap - StripWidth / 2.0f, 0.012f, 0.0f);
        _mesh = CreateStripMesh();
        _strip = new Node3D { Name = "FrequencyStrip" };
        AddChild(_strip);

        _markerMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.3f, 0.8f, 1.0f),
        };
        _markerMesh = new BoxMesh { Size = new Vector3(StripWidth, 0.002f, 0.088f) };
        _currentTimeMarker = new MeshInstance3D
        {
            Name = "CurrentTimeMarker",
            Mesh = _markerMesh,
            MaterialOverride = _markerMaterial,
            Position = new Vector3(0.0f, 0.004f, 0.0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false,
        };
        AddChild(_currentTimeMarker);

        _beatMapManager = GetNode<BeatMapManager>("/root/BeatMapManager");
        _beatMapManager.CurrentBeatmapInfoChanged += OnCurrentBeatmapInfoChanged;
        OnCurrentBeatmapInfoChanged(_beatMapManager.CurrentBeatmapInfo);
    }

    private static ArrayMesh CreateStripMesh()
    {
        using var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = new Vector3[]
        {
            new(-0.5f, 0.0f, 0.5f), new(0.5f, 0.0f, 0.5f),
            new(0.5f, 0.0f, -0.5f), new(-0.5f, 0.0f, -0.5f),
        };
        // Increasing texture V follows song time into the lane (-Z).
        arrays[(int)Mesh.ArrayType.TexUV] = new Vector2[]
        {
            new(0.0f, 0.0f), new(1.0f, 0.0f), new(1.0f, 1.0f), new(0.0f, 1.0f),
        };
        arrays[(int)Mesh.ArrayType.Index] = new int[] { 0, 2, 1, 0, 3, 2 };
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    public override void _Process(double delta)
    {
        if (_analysisTask is not null && _analysisTask.IsCompleted)
        {
            var result = _analysisTask.GetAwaiter().GetResult();
            CancelAnalysis();
            if (result?.Error is not null)
            {
                _analysisFailed = true;
                GD.PushWarning($"Unable to generate song spectrogram: {result.Error}");
            }
            else if (result?.Pixels is not null)
            {
                _songDuration = result.Duration;
                AddTile(result);
            }
        }

        RequestNextTile();
    }

    private void AddTile(AnalysisResult result)
    {
        using var image = Image.CreateFromData(SpectrogramAnalysis.FrequencyBands, SpectrogramAnalysis.RowsPerTile, false, Image.Format.Rgb8, result.Pixels);
        var texture = ImageTexture.CreateFromImage(image);
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            TextureRepeat = false,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlbedoTexture = texture,
        };
        var mesh = new MeshInstance3D
        {
            Name = $"Tile{result.TileIndex}",
            Mesh = _mesh,
            MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false,
        };
        _strip.AddChild(mesh);
        _tiles.Add(result.TileIndex, new TextureTile(mesh, material, texture));
    }

    private void RequestNextTile()
    {
        if (_analysisTask is not null || _analysisFailed || _firstRequiredTile < 0)
        {
            return;
        }

        for (var index = _firstVisibleTile; index <= _lastVisibleTile; index++)
        {
            if (!_tiles.ContainsKey(index))
            {
                StartAnalysis(index);
                return;
            }
        }

        for (var index = _firstRequiredTile; index <= _lastRequiredTile; index++)
        {
            if (!_tiles.ContainsKey(index))
            {
                StartAnalysis(index);
                return;
            }
        }
    }

    public void UpdateWindow(double startTime, double endTime, double playbackTime, float noteSpeed)
    {
        startTime = Math.Clamp(startTime, 0.0, _songDuration);
        endTime = Math.Clamp(endTime, 0.0, _songDuration);
        var visible = endTime > startTime && noteSpeed > 0.0f && IsVisibleInTree();
        _firstVisibleTile = visible ? (int)Math.Floor(startTime / SpectrogramAnalysis.TileDuration) : -1;
        _lastVisibleTile = visible ? (int)Math.Ceiling(endTime / SpectrogramAnalysis.TileDuration) - 1 : -1;
        _firstRequiredTile = visible ? Math.Max(0, _firstVisibleTile - 1) : -1;
        _lastRequiredTile = visible
            ? Math.Min((int)Math.Ceiling(_songDuration / SpectrogramAnalysis.TileDuration) - 1, _lastVisibleTile + 1)
            : -1;

        if (_songDuration > 0.0 && _analysisTask is not null
            && (!visible || _pendingTile < _firstRequiredTile || _pendingTile > _lastRequiredTile))
        {
            CancelAnalysis();
        }

        _currentTimeMarker.Visible = visible && playbackTime >= startTime && playbackTime <= endTime
            && _tiles.ContainsKey(Math.Min(_lastVisibleTile, (int)(playbackTime / SpectrogramAnalysis.TileDuration)));
        foreach (var (index, tile) in _tiles)
        {
            var tileStart = index * SpectrogramAnalysis.TileDuration;
            var start = Math.Max(startTime, tileStart);
            var end = Math.Min(endTime, tileStart + SpectrogramAnalysis.TileDuration);
            tile.Mesh.Visible = visible && end > start;
            if (!tile.Mesh.Visible)
            {
                continue;
            }

            tile.Mesh.Scale = new Vector3(StripWidth, 1.0f, (float)((end - start) * noteSpeed));
            tile.Mesh.Position = new Vector3(0.0f, 0.0f, (float)((playbackTime - (start + end) / 2.0) * noteSpeed));
            tile.Material.Uv1Offset = new Vector3(0.0f, (float)((start - tileStart) / SpectrogramAnalysis.TileDuration), 0.0f);
            tile.Material.Uv1Scale = new Vector3(1.0f, (float)((end - start) / SpectrogramAnalysis.TileDuration), 1.0f);
        }

        EvictDistantTiles();
    }

    private void EvictDistantTiles()
    {
        // Retain the visible window plus prefetch, even at unusually low BPM.
        var capacity = Math.Max(MaximumCachedTiles, _lastRequiredTile - _firstRequiredTile + 1);
        while (_tiles.Count > capacity)
        {
            var candidate = -1;
            var greatestDistance = -1.0;
            foreach (var index in _tiles.Keys)
            {
                if (index >= _firstRequiredTile && index <= _lastRequiredTile)
                {
                    continue;
                }

                var distance = Math.Abs(index - (_firstRequiredTile + _lastRequiredTile) / 2.0);
                if (distance > greatestDistance)
                {
                    candidate = index;
                    greatestDistance = distance;
                }
            }

            if (candidate < 0)
            {
                break;
            }

            ReleaseTile(_tiles[candidate]);
            _tiles.Remove(candidate);
        }
    }

    private void ReleaseTile(TextureTile tile)
    {
        tile.Mesh.Mesh = null;
        tile.Mesh.MaterialOverride = null;
        _strip.RemoveChild(tile.Mesh);
        tile.Mesh.QueueFree();
        tile.Material.AlbedoTexture = null;
        tile.Texture.Dispose();
        tile.Material.Dispose();
    }

    private void ClearTiles()
    {
        foreach (var tile in _tiles.Values)
        {
            ReleaseTile(tile);
        }

        _tiles.Clear();
    }

    private void OnCurrentBeatmapInfoChanged(BeatMapInfo beatmapInfo)
    {
        CancelAnalysis();
        ClearTiles();
        _currentTimeMarker.Hide();
        _songDuration = 0.0;
        _firstVisibleTile = _lastVisibleTile = _firstRequiredTile = _lastRequiredTile = -1;
        _analysisFailed = false;
        _songPath = beatmapInfo?.SongFilePath;
        if (beatmapInfo is null)
        {
            return;
        }

        _sampleRate = AudioServer.GetMixRate();
        StartAnalysis(0);
    }

    private void StartAnalysis(int tileIndex)
    {
        var songPath = _songPath;
        var sampleRate = _sampleRate;
        _analysisCancellation = new CancellationTokenSource();
        var token = _analysisCancellation.Token;
        _pendingTile = tileIndex;
        _analysisTask = Task.Run(() => AnalyzeTile(songPath, sampleRate, tileIndex, token));
    }

    private static AnalysisResult AnalyzeTile(string songPath, double sampleRate, int tileIndex, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            using var stream = AudioStreamOggVorbis.LoadFromFile(songPath)
                ?? throw new InvalidOperationException("The song could not be decoded as Ogg Vorbis.");
            stream.Loop = false;
            var duration = stream.GetLength();
            if (!double.IsFinite(duration) || duration <= 0.0)
            {
                return null;
            }

            using var playback = stream.InstantiatePlayback();
            playback.Start();
            try
            {
                var analysis = new SpectrogramAnalysis(sampleRate);
                var left = new float[SpectrogramAnalysis.WindowSize];
                var right = new float[SpectrogramAnalysis.WindowSize];
                var pixels = new byte[SpectrogramAnalysis.RowsPerTile * SpectrogramAnalysis.FrequencyBands * 3];
                long previousStart = -SpectrogramAnalysis.WindowSize;
                for (var row = 0; row < SpectrogramAnalysis.RowsPerTile; row++)
                {
                    token.ThrowIfCancellationRequested();
                    var centerTime = SpectrogramAnalysis.GetRowCenterTime(tileIndex, row);
                    var start = (long)(centerTime * sampleRate) - SpectrogramAnalysis.WindowSize / 2;
                    ReadWindow(playback, sampleRate, start, previousStart, left, right);
                    previousStart = start;
                    analysis.WriteRow(left, right, pixels.AsSpan(row * SpectrogramAnalysis.FrequencyBands * 3, SpectrogramAnalysis.FrequencyBands * 3));
                }

                return new AnalysisResult(pixels, tileIndex, duration);
            }
            finally
            {
                playback.Stop();
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception exception)
        {
            return new AnalysisResult(null, 0, 0.0, exception.Message);
        }
    }

    private static void ReadWindow(AudioStreamPlayback playback, double sampleRate, long start, long previousStart, float[] left, float[] right)
    {
        var overlap = (int)Math.Clamp(previousStart + SpectrogramAnalysis.WindowSize - start, 0, SpectrogramAnalysis.WindowSize);
        if (overlap > 0)
        {
            Array.Copy(left, left.Length - overlap, left, 0, overlap);
            Array.Copy(right, right.Length - overlap, right, 0, overlap);
        }
        else
        {
            playback.Seek(Math.Max(0.0, start / sampleRate));
        }

        Array.Clear(left, overlap, left.Length - overlap);
        Array.Clear(right, overlap, right.Length - overlap);
        var offset = Math.Max(overlap, (int)Math.Max(0, -start));
        while (offset < left.Length)
        {
            var frames = playback.MixAudio(1.0f, left.Length - offset);
            if (frames.Length == 0)
            {
                break;
            }

            for (var index = 0; index < frames.Length; index++)
            {
                left[offset + index] = frames[index].X;
                right[offset + index] = frames[index].Y;
            }

            offset += frames.Length;
        }
    }

    private void CancelAnalysis()
    {
        _analysisCancellation?.Cancel();
        _analysisCancellation?.Dispose();
        _analysisCancellation = null;
        _analysisTask = null;
        _pendingTile = -1;
    }

    public override void _ExitTree()
    {
        _beatMapManager.CurrentBeatmapInfoChanged -= OnCurrentBeatmapInfoChanged;
        CancelAnalysis();
        ClearTiles();
        _currentTimeMarker.Mesh = null;
        _currentTimeMarker.MaterialOverride = null;
        _mesh.Dispose();
        _markerMesh.Dispose();
        _markerMaterial.Dispose();
    }
}
