using HexCell = PengSim.BL.Model.HexCell;
using PengSim.UI.Map;

namespace PengSim.UI.Rendering;

/// <summary>A rectangular panel measured in terminal character cells.</summary>
internal readonly record struct FrameRect(int X, int Y, int Width, int Height)
{
    // Panel borders use one character on each side.
    public MapScreenBounds CenteredContent(HexCell hexCenter) =>
        MapScreenBounds.Centered(
            2 * hexCenter.Q + hexCenter.R, hexCenter.R,
            Math.Max(1, Width - 2), Math.Max(1, Height - 2));
}

/// <summary>
/// Single source of truth for terminal panel sizes, overview fitting and
/// navigation stride. It depends only on terminal dimensions, not on penguins.
/// </summary>
internal readonly record struct FrameLayout(
    int Width,
    int Height,
    FrameRect Iteration,
    FrameRect Map,
    FrameRect Main,
    FrameRect Minimap,
    FrameRect? Information)
{
    public const string LegendTitle = "Penguins per hex";
    private const int EdgeMargin = 1; // Avoid terminal auto-wrap at bottom/right.
    private const int SideBySideAt = 62;
    private const int MainWidthPercent = 52;
    private const int MinMainWidth = 16;
    private const int PanelGap = 1;
    private const int MinMainHeight = 5;
    private const int MinMinimapHeight = 3;

    public bool Fits => Width >= 24 && Height >= 14 && Main.Height >= 3 && Minimap.Height >= 3;
    public bool InlineLegend => FitsInlineLegend(Map.Width);
    public int LegendColumns => GetLegendColumns(Map.Width);
    public int LegendRow => Map.Y + Map.Height;

    /// <summary>Normal pan moves about one sixth of the visible display.</summary>
    public int PanStride => Math.Max(1,
        Math.Min((Main.Width - 2) / 2, Main.Height - 2) / 6);

    public static FrameLayout Create(int terminalWidth, int terminalHeight)
    {
        var width = Math.Max(0, terminalWidth - EdgeMargin);
        var height = Math.Max(0, terminalHeight - EdgeMargin);

        // Reserve space for the outer border, two metadata lines, and footer.
        var iteration = new FrameRect(2, 3, width - 4, height - 5);
        var map = new FrameRect(3, 4, iteration.Width - 2, iteration.Height - 4);

        if (!FitsInlineLegend(map.Width))
        {
            var columns = GetLegendColumns(map.Width);
            var bandRows = (DensityBand.All.Length + columns - 1) / columns;
            map = map with { Height = map.Height - bandRows };
        }

        if (width >= SideBySideAt)
        {
            var mainWidth = Math.Max(MinMainWidth, map.Width * MainWidthPercent / 100);
            var miniWidth = map.Width - mainWidth - PanelGap;
            var infoHeight = Math.Clamp(map.Height / 5, 5, 8);
            var miniHeight = map.Height - infoHeight;
            var miniX = map.X + mainWidth + PanelGap;

            return new FrameLayout(width, height, iteration, map,
                new FrameRect(map.X, map.Y, mainWidth, map.Height),
                new FrameRect(miniX, map.Y, miniWidth, miniHeight),
                new FrameRect(miniX, map.Y + miniHeight, miniWidth, infoHeight));
        }

        // Narrow terminals stack the main map and the minimap.
        var mainHeight = Math.Clamp(map.Height * 2 / 3, MinMainHeight, Math.Max(MinMainHeight, map.Height - MinMinimapHeight));
        var minimapHeight = map.Height - mainHeight;
        return new FrameLayout(width, height, iteration, map,
            new FrameRect(map.X, map.Y, map.Width, mainHeight),
            new FrameRect(map.X, map.Y + mainHeight, map.Width, minimapHeight),
            null);
    }

    /// <summary>Select the finest complete-world zoom level fitting both maps.</summary>
    public (int Level, HexRegion Region) FitOverview(IHexHeatmap heatmap)
    {
        var availableWidth = Math.Max(1, Math.Min(Map.Width, Minimap.Width) - 2);
        var availableHeight = Math.Max(1, Math.Min(Map.Height, Minimap.Height) - 2);

        // An axial hex of radius R spans 4R+1 columns and 2R+1 rows.
        // Reserve an extra cell for the teal world-border dots.
        var radius = Math.Max(0,
            Math.Min((availableWidth - 3) / 4, (availableHeight - 3) / 2));
        return heatmap.FitWorld(radius);
    }

    private static bool FitsInlineLegend(int width)
    {
        return width >= LegendTitle.Length + 2 + DensityBand.All.Length * DensityBand.LegendColumnWidth;
    }

    private static int GetLegendColumns(int width)
    {
        return Math.Max(1, width / DensityBand.LegendColumnWidth);
    }
}
