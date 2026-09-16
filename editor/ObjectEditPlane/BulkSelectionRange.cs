using System;

public readonly struct BulkSelectionRange
{
    private const double BeatTolerance = 0.000001;

    public BulkSelectionRange(int startColumn, int startRow, int endColumn, int endRow, double startBeat, double endBeat)
    {
        MinColumn = Math.Min(startColumn, endColumn);
        MaxColumn = Math.Max(startColumn, endColumn);
        MinRow = Math.Min(startRow, endRow);
        MaxRow = Math.Max(startRow, endRow);
        MinBeat = Math.Min(startBeat, endBeat);
        MaxBeat = Math.Max(startBeat, endBeat);
    }

    public int MinColumn { get; }
    public int MaxColumn { get; }
    public int MinRow { get; }
    public int MaxRow { get; }
    public double MinBeat { get; }
    public double MaxBeat { get; }

    public bool Contains(BeatMapObjectBase data)
    {
        return data switch
        {
            BeatMapNote note => Overlaps(note.LineIndex, note.LineLayer, 1, 1, note.Beat, note.Beat),
            BeatMapBomb bomb => Overlaps(bomb.LineIndex, bomb.LineLayer, 1, 1, bomb.Beat, bomb.Beat),
            BeatMapWall wall => Overlaps(
                wall.LineIndex,
                wall.LineLayer + (wall.Type == BeatMapWall.WallType.Crouch ? 2 : 0),
                wall.Width,
                wall.Height,
                wall.Beat,
                wall.Beat + wall.Duration),
            _ => false,
        };
    }

    private bool Overlaps(int column, int row, int width, int height, double startBeat, double endBeat)
    {
        return width > 0 && height > 0
            && column <= MaxColumn && column + width - 1 >= MinColumn
            && row <= MaxRow && row + height - 1 >= MinRow
            && startBeat <= MaxBeat + BeatTolerance && endBeat >= MinBeat - BeatTolerance;
    }
}
