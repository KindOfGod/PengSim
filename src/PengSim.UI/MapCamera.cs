namespace PengSim.UI;

internal sealed class MapCamera(HexFootprint footprint)
{
    private HexCell _center;

    public int Zoom { get; private set; }
    public HexRegion View => new(_center, (int)Math.Round(footprint.Radius / Math.Pow(2, Zoom), MidpointRounding.AwayFromZero));

    public void Pan(int horizontal, int vertical)
    {
        if (Zoom == 0)
        {
            return;
        }

        var (column, row) = footprint.ToOffset(_center);
        int step = Math.Max(1, View.Radius / 2);
        for (int i = 0; i < step; i++)
        {
            var next = footprint.ToAxial(column + horizontal, row + vertical);
            if (!footprint.Region.Contains(next))
            {
                break;
            }

            column += horizontal;
            row += vertical;
            _center = next;
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
