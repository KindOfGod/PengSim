namespace PengSim.UI;

internal readonly record struct MapViewport(int Left, int Top, int Width, int Height);

internal sealed class MapCamera(int worldWidth, int worldHeight)
{
    private readonly int _maxZoom = (int)Math.Ceiling(Math.Log2(Math.Max(worldWidth, worldHeight)));

    public int CenterColumn { get; private set; } = worldWidth / 2;
    public int CenterRow { get; private set; } = worldHeight / 2;
    public int Zoom { get; private set; }

    public MapViewport VisibleBounds
    {
        get
        {
            double scale = Math.Pow(2, Zoom);
            int width = Math.Max(1, (int)Math.Ceiling(worldWidth / scale));
            int height = Math.Max(1, (int)Math.Ceiling(worldHeight / scale));

            return new MapViewport(
                Math.Clamp(CenterColumn - width / 2, 0, worldWidth - width),
                Math.Clamp(CenterRow - height / 2, 0, worldHeight - height),
                width,
                height);
        }
    }

    public void Pan(int horizontal, int vertical)
    {
        var viewport = VisibleBounds;
        int left = viewport.Left + horizontal * Math.Max(1, viewport.Width / 4);
        int top = viewport.Top + vertical * Math.Max(1, viewport.Height / 4);

        CenterColumn = Math.Clamp(left, 0, worldWidth - viewport.Width) + viewport.Width / 2;
        CenterRow = Math.Clamp(top, 0, worldHeight - viewport.Height) + viewport.Height / 2;
    }

    public void ChangeZoom(int direction)
    {
        Zoom = Math.Clamp(Zoom + direction, 0, _maxZoom);
    }

    public void Reset()
    {
        CenterColumn = worldWidth / 2;
        CenterRow = worldHeight / 2;
        Zoom = 0;
    }
}
