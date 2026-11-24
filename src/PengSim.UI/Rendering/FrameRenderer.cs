using PengSim.UI.Map;

namespace PengSim.UI.Rendering;

/// <summary>
/// Draws the visible hex map and UI directly into a character buffer.
/// Layout comes from FrameLayout; terminal output comes from TerminalPresenter.
/// </summary>
internal sealed class FrameRenderer(
    IHeatmapPalette palette, IHexGridScale scale, CreateTerminalBuffer createBuffer) : IFrameRenderer
{
    private const uint OuterColor = 0x949494;
    private const uint PanelColor = 0x767676;
    private const uint AccentColor = IHeatmapPalette.Accent;
    private const uint SelectionColor = 0xffd75f;
    private const uint CenterColor = 0xfff0a0;
    private const string HeaderGap = "    ";

    private static readonly (string Key, string Action)[] Shortcuts =
    [
        ("WASD", "pan"),
        ("Shift+WASD", "fine"),
        ("+/-", "zoom"),
        ("←/→", "step"),
        ("Backspace", "rewind"),
        ("0", "pause"),
        ("1-9", "fps"),
        ("R", "reset"),
        ("Q", "quit"),
    ];

    public string[] RenderRows(
        HexFootprint footprint,
        IHexHeatmap heatmap,
        IMapCamera camera,
        string simulationName,
        string parameterCode,
        int iteration,
        int lastIteration,
        int speed,
        FrameLayout layout,
        bool ansi = true)
    {
        if (!layout.Fits)
        {
            return ["Terminal too small for map view. Increase the terminal width or height."];
        }

        var screen = createBuffer(layout.Width, layout.Height);
        screen.Box(0, 0, layout.Width, layout.Height, "Penguin Simulation", OuterColor);
        screen.Text(2, 1, $"Name: {simulationName}{HeaderGap}Code: {parameterCode}",
            IHeatmapPalette.DefaultColor, layout.Width - 4);
        screen.Text(2, 2, speed == 0 ? "Paused" : $"Playing at {speed} fps",
            IHeatmapPalette.DefaultColor, layout.Width - 4);

        var iterationPanel = layout.Iteration;
        screen.Box(iterationPanel.X, iterationPanel.Y,
            iterationPanel.Width, iterationPanel.Height,
            $"Iteration {iteration}/{lastIteration}", AccentColor);

        var overview = layout.FitOverview(heatmap);
        var level = camera.Level ?? overview.Level;
        var mainCenter = camera.Level.HasValue
            ? scale.ToLevel(camera.Center, level)
            : overview.Region.Center;

        DrawMain(screen, footprint, heatmap, level,
            mainCenter, layout.Main);
        DrawMinimap(screen, footprint, heatmap, camera,
            overview.Level, level, mainCenter, layout.Main, layout.Minimap);

        if (layout.Information is { } info)
        {
            DrawInformation(screen, footprint, camera, level, overview.Level, info);
        }

        DrawLegend(screen, layout);

        var (q, r) = footprint.ToMap(camera.Center);
        var position = camera.Level.HasValue
            ? $"Zoom L{level}   (q {q}, r {r})"
            : $"Overview L{overview.Level}   (q {q}, r {r})";
        screen.Text(layout.Map.X, iterationPanel.Y + iterationPanel.Height - 2,
            position, IHeatmapPalette.DefaultColor, layout.Map.Width);
        DrawShortcuts(screen, layout);

        return screen.ToAnsiRows(ansi);
    }

    private static void DrawLegend(ITerminalBuffer screen, FrameLayout layout)
    {
        var left = layout.Map.X;
        var row = layout.LegendRow;
        screen.Text(left, row, FrameLayout.LegendTitle, IHeatmapPalette.DefaultColor, layout.Map.Width);
        if (layout.InlineLegend)
        {
            left += FrameLayout.LegendTitle.Length + 2;
        }
        else
        {
            row++;
        }

        var bands = DensityBand.All;
        var columns = layout.LegendColumns;
        for (var i = 0; i < bands.Length; i++)
        {
            var x = left + i % columns * DensityBand.LegendColumnWidth;
            var y = row + i / columns;
            var band = bands[i];
            screen.Put(x, y, band.Paint);
            screen.Text(x + 2, y, band.Label, IHeatmapPalette.DefaultColor, band.Label.Length);
        }
    }

    private static void DrawShortcuts(ITerminalBuffer screen, FrameLayout layout)
    {
        var x = 2;
        var y = layout.Height - 2;
        var right = layout.Width - 2;
        foreach (var (key, action) in Shortcuts)
        {
            screen.Text(x, y, key, AccentColor, right - x);
            x += key.Length + 1;
            screen.Text(x, y, action, IHeatmapPalette.DefaultColor, right - x);
            x += action.Length + 2;
        }
    }

    private void DrawMain(
        ITerminalBuffer screen, HexFootprint footprint,
        IHexHeatmap heatmap, int level, HexCell center, FrameRect panel)
    {
        screen.Box(panel.X, panel.Y, panel.Width, panel.Height,
            level == 0 ? "Hex grid · L0" : $"Heatmap · L{level}", PanelColor);

        DrawCells(screen, footprint, heatmap, level,
            panel.CenteredContent(center), panel.X + 1, panel.Y + 1,
            null, null);
    }

    private void DrawMinimap(
        ITerminalBuffer screen, HexFootprint footprint,
        IHexHeatmap heatmap, IMapCamera camera,
        int overviewLevel, int detailLevel, HexCell mainCenter,
        FrameRect main, FrameRect minimap)
    {
        screen.Box(minimap.X, minimap.Y, minimap.Width, minimap.Height,
            $"Minimap · L{overviewLevel}", PanelColor);

        // The overview remains centered on the world while the selected
        // rectangle moves with the main camera at the active zoom level.
        var visible = main.CenteredContent(mainCenter);
        var selection = camera.Level.HasValue
            ? MinimapViewport.Project(visible, detailLevel, overviewLevel)
            : (MinimapViewport?)null;
        HexCell? selectedCenter = camera.Level.HasValue
            ? scale.ToLevel(camera.Center, overviewLevel)
            : null;

        DrawCells(screen, footprint, heatmap, overviewLevel,
            minimap.CenteredContent(default), minimap.X + 1, minimap.Y + 1,
            selection, selectedCenter);
    }

    private void DrawInformation(
        ITerminalBuffer screen, HexFootprint footprint, IMapCamera camera,
        int level, int overviewLevel, FrameRect panel)
    {
        screen.Box(panel.X, panel.Y, panel.Width, panel.Height,
            "Information", PanelColor);
        var (q, r) = footprint.ToMap(camera.Center);
        var lines = new[]
        {
            $"Center: q {q}, r {r}",
            camera.Level.HasValue
                ? $"Detail L{level} / overview L{overviewLevel}"
                : $"Full overview L{overviewLevel}",
            "Teal: world border",
            "Yellow: visible area",
        };

        for (var i = 0; i < lines.Length && i < panel.Height - 2; i++)
        {
            screen.Text(panel.X + 1, panel.Y + i + 1, lines[i],
                IHeatmapPalette.DefaultColor, panel.Width - 2);
        }
    }

    /// <summary>
    /// Rasterize only the requested rectangle. Hex cells occur on alternating
    /// screen columns: screenX = 2*q+r, screenY = r. The other columns remain
    /// blank so the hexagonal rows appear staggered in a monospace terminal.
    /// </summary>
    private void DrawCells(
        ITerminalBuffer screen, HexFootprint footprint,
        IHexHeatmap heatmap, int level, MapScreenBounds bounds,
        int screenX, int screenY,
        MinimapViewport? selection, HexCell? cameraCenter)
    {
        var world = level == 0 ? footprint.Region : heatmap.GetRegion(level);

        for (var y = 0; y < bounds.Height; y++)
        {
            var row = bounds.Top + y;

            // Cell centers satisfy mapX = 2*q + row. Start on the matching
            // parity and step over the blank column between neighboring hexes.
            var firstColumn = (row - bounds.Left) & 1;
            for (var x = firstColumn; x < bounds.Width; x += 2)
            {
                var mapX = bounds.Left + x;
                var cell = new HexCell((mapX - row) / 2, row);
                var paint = GetCellPaint(heatmap, world, level, cell);

                if (cameraCenter is { } center && cell == center)
                {
                    paint = new TerminalPixel('●', CenterColor);
                }
                else if (selection is { } selected && selected.IsOnBorder(mapX, row))
                {
                    paint = new TerminalPixel('⬢', SelectionColor);
                }

                screen.Put(screenX + x, screenY + y, paint);
            }
        }
    }

    /// <summary>
    /// L0 reads the selected frame’s occupied cells. Coarser hex cells use the
    /// sparse aggregate index. Only cells at distance R+1 from the *world*
    /// center get the teal border; the camera perimeter never gets it.
    /// </summary>
    private TerminalPixel GetCellPaint(
        IHexHeatmap heatmap,
        HexRegion world, int level, HexCell cell)
    {
        var distance = cell.DistanceTo(world.Center);
        if (distance <= world.Radius)
        {
            var population = heatmap.GetPopulation(level, cell);
            return palette.GetDensityPaint(population);
        }

        return distance == world.Radius + 1
            ? palette.WorldEdgePaint
            : palette.OutsidePaint;
    }
}
