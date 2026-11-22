namespace PengSim.UI;

internal sealed class MapCamera(HexFootprint footprint)
{
    private HexCell _center;

    public int Zoom { get; private set; }
    public HexRegion View => new(_center, (int)Math.Round(footprint.Radius / Math.Pow(2, Zoom), MidpointRounding.AwayFromZero));

    public void Pan(int horizontal, int vertical, int overviewLevel, HexRegion limits)
    {
        var cell = _center.Ancestor(overviewLevel);
        int row = cell.R + vertical;
        int q = cell.Q + horizontal + (cell.R >> 1) - (row >> 1);
        var target = new HexCell(q, row);
        var center = target.DescendToCenter(overviewLevel);
        if (limits.Contains(target) && footprint.Region.Contains(center))
        {
            _center = center;
        }
    }

    public void KeepMarkerInBounds(int overviewLevel, HexRegion limits)
    {
        var cell = _center.Ancestor(overviewLevel);
        if (!limits.Contains(cell))
        {
            _center = limits.Cells()
                .Select(candidate => (Cell: candidate, Center: candidate.DescendToCenter(overviewLevel)))
                .Where(candidate => footprint.Region.Contains(candidate.Center))
                .MinBy(candidate => candidate.Cell.DistanceTo(cell)).Center;
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
