namespace PengSim.UI;

internal sealed class MapCamera(HexFootprint footprint)
{
    private HexCell _center;

    public int Zoom { get; private set; }
    public HexRegion View => new(_center, (int)Math.Round(footprint.Radius / Math.Pow(2, Zoom), MidpointRounding.AwayFromZero));

    public void Pan(int horizontal, int vertical, int overviewLevel, HexHeatmap heatmap)
    {
        var cell = _center.Ancestor(overviewLevel);
        int row = cell.R + vertical;
        int q = cell.Q + horizontal + (cell.R >> 1) - (row >> 1);
        if (heatmap.FindBaseCell(new HexCell(q, row), overviewLevel) is { } center)
        {
            _center = center;
        }
    }

    public void PanFine(int horizontal, int vertical)
    {
        var (column, row) = footprint.ToOffset(_center);
        var target = footprint.ToAxial(column + horizontal, row + vertical);
        if (footprint.Region.Contains(target))
        {
            _center = target;
        }
    }

    public void ChangeZoom(int direction)
    {
        int maxZoom = (int)Math.Ceiling(Math.Log2(Math.Max(1, footprint.Radius))) + 1;
        Zoom = Math.Clamp(Zoom + direction, 0, maxZoom);
        if (Zoom == 0)
        {
            _center = default;
        }
    }

    public void Reset()
    {
        _center = default;
        Zoom = 0;
    }
}
