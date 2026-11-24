namespace PengSim.UI.Map;

/// <summary>
/// Stores the camera in base-level axial coordinates. The display window is
/// rectangular, but the world and the aggregation lattice remain hexagonal.
/// </summary>
internal sealed class MapCamera(HexFootprint footprint, IHexGridScale scale) : IMapCamera
{
    /// <inheritdoc />
    public HexCell Center { get; private set; }

    /// <inheritdoc />
    /// <remarks>Null displays the full overview; L0 displays base hexagons.</remarks>
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
        var (column, row) = ToOffset(current);
        var target = ToAxial(column + horizontal * steps, row + vertical * steps);
        Center = footprint.ClampToBounds(scale.ToBase(target, level));
    }

    /// <summary>
    /// Moves one base cell. Empty corners of the screen rectangle are valid:
    /// only the hexagonal simulation footprint decides what is playable.
    /// </summary>
    public void PanFine(int horizontal, int vertical)
    {
        var (column, row) = ToOffset(Center);
        Center = footprint.ClampToBounds(ToAxial(column + horizontal, row + vertical));
    }

    /// <inheritdoc />
    public void ZoomIn(int overviewLevel)
    {
        Level = Math.Max(0, (Level ?? overviewLevel) - 1);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Reset()
    {
        Center = default;
        Level = null;
    }

    /// <summary>
    /// Converts a cell to staggered offset coordinates, where moving one row up or down keeps
    /// the column and therefore alternates between the two diagonal neighbors.
    /// </summary>
    private static (int Column, int Row) ToOffset(HexCell cell)
    {
        return (cell.Q + (cell.R >> 1), cell.R);
    }

    /// <summary>Converts staggered offset coordinates back to a cell; the inverse of <see cref="ToOffset"/>.</summary>
    private static HexCell ToAxial(int column, int row)
    {
        return new HexCell(column - (row >> 1), row);
    }
}
