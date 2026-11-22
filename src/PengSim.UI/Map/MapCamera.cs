namespace PengSim.UI.Map;

/// <summary>Tracks a base-cell position and an optional zoom level.</summary>
internal sealed class MapCamera(HexFootprint footprint)
{
    /// <summary>Gets the camera position in base-level axial coordinates.</summary>
    public HexCell Center { get; private set; }

    /// <summary>Gets the zoom level, or null when showing the full world.</summary>
    public int? Level { get; private set; }

    /// <summary>Moves one minimap cell and lands on a playable base cell.</summary>
    public void Pan(int horizontal, int vertical, int overviewLevel, HexHeatmap heatmap)
    {
        var cell = Center.Ancestor(overviewLevel);
        var row = cell.R + vertical;
        var q = cell.Q + horizontal + (cell.R >> 1) - (row >> 1);

        if (heatmap.FindBaseCell(new HexCell(q, row), overviewLevel) is { } center)
        {
            Center = center;
        }
    }

    /// <summary>Moves one base cell without leaving the playable region.</summary>
    public void PanFine(int horizontal, int vertical)
    {
        var (column, row) = footprint.ToOffset(Center);
        var target = footprint.ToAxial(column + horizontal, row + vertical);

        if (footprint.Region.Contains(target))
        {
            Center = target;
        }
    }

    /// <summary>Selects the next finer level and stops at actual cells.</summary>
    public void ZoomIn(int overviewLevel)
    {
        Level = Math.Max(0, (Level ?? overviewLevel) - 1);
    }

    /// <summary>Selects the next coarser level or returns to the full overview.</summary>
    public void ZoomOut(int overviewLevel)
    {
        if (Level is not int level)
        {
            return;
        }

        if (level + 1 >= overviewLevel)
        {
            Reset();
        }
        else
        {
            Level = level + 1;
        }
    }

    /// <summary>Centers the camera and returns to the full overview.</summary>
    public void Reset()
    {
        Center = default;
        Level = null;
    }
}
