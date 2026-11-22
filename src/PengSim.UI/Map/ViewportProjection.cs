namespace PengSim.UI.Map;

/// <summary>
/// Terminal character rectangle. Right and Bottom are exclusive.
/// Geometry is purely a UI concern; the world itself remains hexagonal.
/// </summary>
internal readonly record struct MapScreenBounds(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;
    public int Bottom => Top + Height;

    public static MapScreenBounds Centered(int centerX, int centerY, int width, int height) =>
        new(centerX - (width - 1) / 2, centerY - (height - 1) / 2, width, height);
}

/// <summary>
/// A rectangular minimap overlay with a size that depends on zoom, not on
/// the camera position. The border can overlap empty off-world areas.
/// </summary>
internal readonly record struct MinimapViewport(int Left, int Top, int Right, int Bottom)
{
    public bool Contains(int x, int y) =>
        x >= Left && x <= Right && y >= Top && y <= Bottom;

    public bool IsOnBorder(int x, int y) =>
        Contains(x, y) &&
        (x - Left <= 2 || Right - x <= 2 || y == Top || y == Bottom);
}

/// <summary>
/// The hex display levels all share the same physical axes. Converting
/// between them is now an isotropic power-of-two scale, not a rotation.
/// </summary>
internal static class ViewportProjection
{
    public static MinimapViewport Project(
        MapScreenBounds visible, int detailLevel, int minimapLevel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(detailLevel);
        ArgumentOutOfRangeException.ThrowIfNegative(minimapLevel);

        var scale = Math.Pow(2.0, detailLevel - minimapLevel);
        var centerX = (int)Math.Round((visible.Left + (visible.Width - 1) / 2.0) * scale);
        var centerY = (int)Math.Round((visible.Top + (visible.Height - 1) / 2.0) * scale);

        // Quantize sizes independently of center position, so panning cannot
        // stretch or deform the marker.
        var halfWidth = Math.Max(1, (int)Math.Ceiling((visible.Width - 1) * scale / 2.0));
        var halfHeight = Math.Max(1, (int)Math.Ceiling((visible.Height - 1) * scale / 2.0));

        return new MinimapViewport(
            centerX - halfWidth, centerY - halfHeight,
            centerX + halfWidth, centerY + halfHeight);
    }
}
