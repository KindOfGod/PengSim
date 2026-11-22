using PengSim.UI.Map;

namespace PengSim.UI.Rendering;

/// <summary>Describes the panel space available inside the application and iteration frames.</summary>
internal readonly record struct TerminalLayout(
    int ContentWidth,
    int MainWidth,
    int MainRadius,
    int MiniWidth,
    int MiniRadius,
    int SideHeight,
    bool SideBySide,
    bool ShowControls,
    bool ShowScale)
{
    /// <summary>Gets the map radius after reserving a ring for the outline.</summary>
    public int DetailRadius => Math.Max(0, MainRadius - 1);

    /// <summary>Allocates space for the main map, side panels, legend, and controls.</summary>
    public static TerminalLayout Fit(int terminalWidth, int terminalHeight, bool interactive)
    {
        const int frameColumns = 6;
        const int frameAndDetailsRows = 6;
        const int informationRows = 3;
        const int fallbackHeight = 24;

        // The outer frame adds horizontal padding. Both frames and the metadata take rows.
        var width = Math.Max(1, terminalWidth - frameColumns);
        var height = Math.Max(1, (terminalHeight > 0 ? terminalHeight : fallbackHeight) - frameAndDetailsRows);
        var sideBySide = width >= 38;
        var showControls = interactive && width >= 26 && height >= (sideBySide ? 9 : 14);
        var showScale = width >= HeatmapPalette.BandCount && height >= (sideBySide ? 7 : 11);

        var mapHeight = height - (showControls ? 2 : 0) - (showScale ? 1 : 0);
        var miniWidth = sideBySide ? width * 48 / 100 : width;
        var sideHeight = sideBySide ? mapHeight : Math.Max(6, mapHeight / 2);
        var mainWidth = sideBySide ? width - miniWidth - 1 : width;
        var mainHeight = sideBySide ? mapHeight : mapHeight - sideHeight;

        return new TerminalLayout(
            width, mainWidth, FittingRadius(mainWidth, mainHeight),
            miniWidth, FittingRadius(miniWidth, sideHeight - informationRows), sideHeight,
            sideBySide, showControls, showScale);
    }

    /// <summary>Chooses a full-world level that fits both the overview and the minimap.</summary>
    public (int Level, HexRegion Region) FitOverview(HexHeatmap heatmap) =>
        heatmap.FitWorld(Math.Min(DetailRadius, MiniRadius));

    /// <summary>Finds the largest hex whose glyphs, spacing, and border fit the panel.</summary>
    private static int FittingRadius(int width, int height) =>
        Math.Max(0, Math.Min((width - 5) / 4, (height - 3) / 2));
}
