namespace PengSim.UI.Map;

internal sealed class MapCamera(HexFootprint footprint)
{
    public HexCell Center { get; private set; }
    public int? Level { get; private set; }

    public void Pan(int horizontal, int vertical, int overviewLevel, HexHeatmap heatmap)
    {
        var cell = Center.Ancestor(overviewLevel);
        int row = cell.R + vertical;
        int q = cell.Q + horizontal + (cell.R >> 1) - (row >> 1);
        if (heatmap.FindBaseCell(new HexCell(q, row), overviewLevel) is { } center)
        {
            Center = center;
        }
    }

    public void PanFine(int horizontal, int vertical)
    {
        var (column, row) = footprint.ToOffset(Center);
        var target = footprint.ToAxial(column + horizontal, row + vertical);
        if (footprint.Region.Contains(target))
        {
            Center = target;
        }
    }

    public void ZoomIn(int overviewLevel)
    {
        Level = Math.Max(0, (Level ?? overviewLevel) - 1);
    }

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

    public void Reset()
    {
        Center = default;
        Level = null;
    }
}
