namespace PengSim.UI.Map;

/// <summary>
/// Stores the camera in base-level axial coordinates. The display window is
/// rectangular, but the world and the aggregation lattice remain hexagonal.
/// </summary>
internal sealed class MapCamera(HexFootprint footprint, IHexGridScale scale) : IMapCamera
{
    public HexCell Center { get; private set; }

    /// <summary>Null displays the full overview; L0 displays base hexagons.</summary>
    public int? Level { get; private set; }

    /// <summary>
    /// Moves in the currently displayed hex lattice, keeping horizontal and
    /// vertical keys aligned with the screen's staggered row directions.
    /// </summary>
    public void Pan(int horizontal, int vertical, int level, int steps)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steps);

        var current = scale.ToLevel(Center, level);
        var nextRow = current.R + vertical * steps;
        var nextColumn = current.Q + horizontal * steps + (current.R >> 1) - (nextRow >> 1);
        var target = scale.ToBase(new HexCell(nextColumn, nextRow), level);
        MoveToStorage(target);
    }

    /// <summary>
    /// Moves one base cell. Empty corners of the storage square are valid:
    /// only the hexagonal simulation footprint decides what is playable.
    /// </summary>
    public void PanFine(int horizontal, int vertical)
    {
        var (column, row) = footprint.ToOffset(Center);
        var next = footprint.ToAxial(column + horizontal, row + vertical);
        MoveToStorage(next);
    }

    public void ZoomIn(int overviewLevel) =>
        Level = Math.Max(0, (Level ?? overviewLevel) - 1);

    public void ZoomOut(int overviewLevel)
    {
        if (Level is not int current)
        {
            return;
        }

        if (current + 1 >= overviewLevel)
        {
            Reset();
        }
        else
        {
            Level = current + 1;
        }
    }

    public void Reset()
    {
        Center = default;
        Level = null;
    }

    private void MoveToStorage(HexCell requested)
    {
        var (column, row) = footprint.ToOffset(requested);

        column = Math.Clamp(column, 0, footprint.Size - 1);
        row = Math.Clamp(row, 0, footprint.Size - 1);
        Center = footprint.ToAxial(column, row);
    }
}
