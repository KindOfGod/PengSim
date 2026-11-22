using HexCell = PengSim.BL.Model.HexCell;
using PengSim.UI.Map;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Rendering;

/// <summary>
/// Draws the visible hex map and UI directly into a character buffer.
/// Layout comes from FrameLayout; terminal output comes from TerminalPresenter.
/// </summary>
internal static class FrameRenderer
{
    private const uint OuterColor = 0x949494;
    private const uint PanelColor = 0x767676;
    private const uint AccentColor = HeatmapPalette.Accent;
    private const uint SelectionColor = 0xffd75f;
    private const uint CenterColor = 0xfff0a0;

    private const int LegendTitleWidth = 18;
    private const int LegendSwatchSpacing = 2;

    public static string[] RenderRows(
        HexEnvironment environment,
        HexFootprint footprint,
        HexHeatmap heatmap,
        MapCamera camera,
        string simulationName,
        int iteration,
        int lastIteration,
        int speed,
        FrameLayout layout,
        bool ansi = true)
    {
        if (!layout.Fits)
        {
            return ["Terminal too small for map view. Resize to at least 25 x 15."];
        }

        var screen = new TerminalBuffer(layout.Width, layout.Height);
        screen.Box(0, 0, layout.Width, layout.Height, "Penguin Simulation", OuterColor);
        screen.Text(2, 1, "Name: " + simulationName,
            HeatmapPalette.DefaultColor, layout.Width - 4);
        screen.Text(2, 2, speed == 0 ? "Paused" : $"Playing at {speed} fps",
            HeatmapPalette.DefaultColor, layout.Width - 4);

        var iterationPanel = layout.Iteration;
        screen.Box(iterationPanel.X, iterationPanel.Y,
            iterationPanel.Width, iterationPanel.Height,
            $"Iteration {iteration}/{lastIteration}", AccentColor);

        var overview = layout.FitOverview(heatmap);
        var level = camera.Level ?? overview.Level;
        var mainCenter = camera.Level.HasValue
            ? HexGridScale.ToLevel(camera.Center, level)
            : overview.Region.Center;

        DrawMain(screen, environment, footprint, heatmap, level,
            mainCenter, layout.Main);
        DrawMinimap(screen, environment, footprint, heatmap, camera,
            overview.Level, level, mainCenter, layout.Main, layout.Minimap);

        if (layout.Information is { } info)
        {
            DrawInformation(screen, footprint, camera, level, overview.Level, info);
        }

        var legendY = layout.LegendRow;
        screen.Text(layout.Map.X, legendY, "Penguins per hex",
            HeatmapPalette.DefaultColor, LegendTitleWidth);

        for (var i = 0; i < HeatmapPalette.DensityBands.Length; i++)
        {
            var x = layout.Map.X + LegendTitleWidth + i * LegendSwatchSpacing;
            if (x >= layout.Map.X + layout.Map.Width)
            {
                break;
            }

            screen.Put(x, legendY, HeatmapPalette.DensityBands[i]);
        }

        var (column, row) = footprint.ToOffset(camera.Center);
        var position = camera.Level.HasValue
            ? $"Zoom L{level}   ({column}, {row})"
            : $"Overview L{overview.Level}   ({column}, {row})";
        screen.Text(layout.Map.X, legendY + 1,
            position, HeatmapPalette.DefaultColor, layout.Map.Width);
        screen.Text(2, layout.Height - 2,
            "WASD pan  Shift+WASD fine  +/- zoom  ←/→ step  Home start  0 pause  1-9 fps  R reset  Q quit",
            HeatmapPalette.DefaultColor, layout.Width - 4);

        return screen.ToAnsiRows(ansi);
    }

    private static void DrawMain(
        TerminalBuffer screen, HexEnvironment environment, HexFootprint footprint,
        HexHeatmap heatmap, int level, HexCell center, FrameRect panel)
    {
        screen.Box(panel.X, panel.Y, panel.Width, panel.Height,
            level == 0 ? "Hex grid · L0" : $"Heatmap · L{level}", PanelColor);

        DrawCells(screen, environment, footprint, heatmap, level,
            panel.CenteredContent(center), panel.X + 1, panel.Y + 1,
            null, null);
    }

    private static void DrawMinimap(
        TerminalBuffer screen, HexEnvironment environment, HexFootprint footprint,
        HexHeatmap heatmap, MapCamera camera,
        int overviewLevel, int detailLevel, HexCell mainCenter,
        FrameRect main, FrameRect minimap)
    {
        screen.Box(minimap.X, minimap.Y, minimap.Width, minimap.Height,
            $"Minimap · L{overviewLevel}", PanelColor);

        // The overview remains centered on the world while the selected
        // rectangle moves with the main camera at the active zoom level.
        var visible = main.CenteredContent(mainCenter);
        var selection = camera.Level.HasValue
            ? ViewportProjection.Project(visible, detailLevel, overviewLevel)
            : (MinimapViewport?)null;
        HexCell? selectedCenter = camera.Level.HasValue
            ? HexGridScale.ToLevel(camera.Center, overviewLevel)
            : null;

        DrawCells(screen, environment, footprint, heatmap, overviewLevel,
            minimap.CenteredContent(default), minimap.X + 1, minimap.Y + 1,
            selection, selectedCenter);
    }

    private static void DrawInformation(
        TerminalBuffer screen, HexFootprint footprint, MapCamera camera,
        int level, int overviewLevel, FrameRect panel)
    {
        screen.Box(panel.X, panel.Y, panel.Width, panel.Height,
            "Information", PanelColor);
        var (column, row) = footprint.ToOffset(camera.Center);
        var lines = new[]
        {
            $"Center: ({column},{row})",
            camera.Level.HasValue
                ? $"Detail L{level} / overview L{overviewLevel}"
                : $"Full overview L{overviewLevel}",
            "Teal: world border",
            "Yellow: visible area",
        };

        for (var i = 0; i < lines.Length && i < panel.Height - 2; i++)
        {
            screen.Text(panel.X + 1, panel.Y + i + 1, lines[i],
                HeatmapPalette.DefaultColor, panel.Width - 2);
        }
    }

    /// <summary>
    /// Rasterize only the requested rectangle. Hex cells occur on alternating
    /// screen columns: screenX = 2*q+r, screenY = r. The other columns remain
    /// blank so the hexagonal rows appear staggered in a monospace terminal.
    /// </summary>
    private static void DrawCells(
        TerminalBuffer screen, HexEnvironment environment, HexFootprint footprint,
        HexHeatmap heatmap, int level, MapScreenBounds bounds,
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
                var paint = GetCellPaint(environment, footprint, heatmap, world, level, cell);

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
    /// L0 reads the current simulation directly. Coarser hex cells use the
    /// sparse aggregate index. Only cells at distance R+1 from the *world*
    /// center get the teal border; the camera perimeter never gets it.
    /// </summary>
    private static TerminalPixel GetCellPaint(
        HexEnvironment environment, HexFootprint footprint, HexHeatmap heatmap,
        HexRegion world, int level, HexCell cell)
    {
        var distance = cell.DistanceTo(world.Center);
        if (distance <= world.Radius)
        {
            var population = level == 0
                ? BasePopulation(environment, footprint, cell)
                : heatmap.GetPopulation(level, cell);
            return HeatmapPalette.GetDensityPaint(population);
        }

        return distance == world.Radius + 1
            ? HeatmapPalette.WorldEdgePaint
            : HeatmapPalette.OutsidePaint;
    }

    private static int BasePopulation(
        HexEnvironment environment, HexFootprint footprint, HexCell cell)
    {
        var (column, row) = footprint.ToOffset(cell);
        return environment[column, row] == HexEnvironment.EmptyCell ? 0 : 1;
    }

}
