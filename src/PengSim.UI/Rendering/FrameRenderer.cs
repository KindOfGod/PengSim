using System.Text;
using PengSim.UI.Map;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Rendering;

/// <summary>
/// Creates one complete terminal frame from the current camera and simulation.
/// The caller publishes the completed rows in a single buffered update.
///
/// Layout coordinates are terminal character cells (x increases rightward,
/// y increases downward), not hexagonal map coordinates. This is why the view
/// windows are rectangular although every map cell and world boundary is hexagonal.
///
/// No Spectre layout tree is built per frame: only currently visible map cells
/// are sampled, then the finished frame is encoded as ANSI-colored text rows.
/// </summary>
internal static class FrameRenderer
{
    // All sizes below are measured in terminal *character cells*, not hex cells.
    // Leave one terminal row/column unused to avoid auto-wrap and scroll on
    // terminals that scroll when the final cell in the last row is written.
    private const int TerminalEdgeMargin = 1;
    private const int MinimumWidth = 24;
    private const int MinimumHeight = 14;

    // Outer panel, metadata and iteration panel occupy the top/left padding.
    private const int OuterInset = 2;
    private const int SimulationNameRow = 1;
    private const int SimulationPathRow = 2;
    private const int IterationPanelTop = 3;
    private const int IterationPanelBottomReserve = 5;
    private const int IterationContentInset = 1;
    private const int IterationFooterReserve = 4; // map bottom, legend and status

    // The main panel occupies 52% of the map width when there is a sidebar.
    // Below this threshold, main and minimap stack vertically instead.
    private const int SideBySideWidth = 62;
    private const int MainWidthPercent = 52;
    private const int PercentageBase = 100;
    private const int MinimumMainWidth = 16;
    private const int PanelGap = 1;

    // Reserve roughly 20% of the sidebar for the Information box, bounded
    // so the minimap remains visible in terminals of different heights.
    private const int InformationHeightDivisor = 5;
    private const int MinimumInformationHeight = 5;
    private const int MaximumInformationHeight = 8;
    private const int MinimumStackedMainHeight = 5;
    private const int MinimumStackedMinimapHeight = 3;

    // In this glyph projection, adjacent hex centers are two text columns
    // apart; the other columns preserve the staggered hex-grid appearance.
    private const int HexColumnSpacing = 2;
    private const int LegendTitleWidth = 18;
    private const int PanStrideDivisor = 6;

    // RGB values use the 0xRRGGBB format; the default color is a sentinel.
    private const uint OuterColor = 0x949494;
    private const uint PanelColor = 0x767676;
    private const uint AccentColor = HeatmapPalette.Accent;
    private const uint YellowColor = 0xffd75f;
    private const uint CenterColor = 0xfff0a0;
    // One representative population from each heatmap color band, ordered
    // exactly like HeatmapPalette. The legend is a swatch, not a second map.
    private static readonly int[] LegendCounts = [0, 1, 2, 4, 7, 15, 31, 63, 127, 128];

    /// <summary>
    /// Renders one complete frame. Plain text output is used when stdout is
    /// redirected; interactive output contains ANSI foreground-color codes.
    /// </summary>
    public static string[] RenderRows(
        HexEnvironment environment,
        HexFootprint footprint,
        HexHeatmap heatmap,
        MapCamera camera,
        string simulationName,
        string? outputPath,
        int iteration,
        int terminalWidth,
        int terminalHeight,
        bool ansi = true)
    {
        var width = Math.Max(0, terminalWidth - TerminalEdgeMargin);
        var height = Math.Max(0, terminalHeight - TerminalEdgeMargin);
        if (width < MinimumWidth || height < MinimumHeight)
        {
            return ["Terminal too small for map view. Resize to at least 25 x 15."];
        }

        var screen = new Screen(width, height);
        screen.Box(0, 0, width, height, "Penguin Simulation", OuterColor);
        screen.Text(OuterInset, SimulationNameRow,
            "Name: " + simulationName, HeatmapPalette.DefaultColor, width - 2 * OuterInset);
        screen.Text(OuterInset, SimulationPathRow,
            "Path: " + (outputPath ?? "Unsaved"), HeatmapPalette.DefaultColor, width - 2 * OuterInset);

        // Nested panels are positioned by their outer border. Each inner
        // rectangle is inset by one cell so it cannot overwrite that border.
        var iterationX = OuterInset;
        var iterationY = IterationPanelTop;
        var iterationWidth = width - 2 * OuterInset;
        var iterationHeight = height - IterationPanelBottomReserve;
        screen.Box(iterationX, iterationY, iterationWidth, iterationHeight,
            $"Iteration {iteration}", AccentColor);

        var mapX = iterationX + IterationContentInset;
        var mapY = iterationY + IterationContentInset;
        var mapWidth = iterationWidth - 2 * IterationContentInset;
        var mapHeight = iterationHeight - IterationFooterReserve;
        var overview = FitOverview(heatmap, terminalWidth, terminalHeight);
        var level = camera.Level ?? overview.Level;
        var mainCenter = camera.Level.HasValue
            ? HexGridScale.ToLevel(camera.Center, level)
            : overview.Region.Center;

        if (width >= SideBySideWidth)
        {
            // Wide terminals: main map on the left; overview and Information
            // share the right column. The one-cell gap separates the panels.
            var mainWidth = Math.Max(MinimumMainWidth,
                mapWidth * MainWidthPercent / PercentageBase);
            var miniWidth = mapWidth - mainWidth - PanelGap;
            var infoHeight = Math.Clamp(mapHeight / InformationHeightDivisor,
                MinimumInformationHeight, MaximumInformationHeight);
            var miniHeight = mapHeight - infoHeight;
            var miniX = mapX + mainWidth + PanelGap;

            DrawMain(screen, environment, footprint, heatmap, level, mainCenter,
                mapX, mapY, mainWidth, mapHeight);
            DrawMinimap(screen, environment, footprint, heatmap, camera,
                overview.Level, level, mainCenter,
                mainWidth, mapHeight,
                miniX, mapY, miniWidth, miniHeight);
            DrawInformation(screen, footprint, camera, level, overview.Level,
                miniX, mapY + miniHeight, miniWidth, infoHeight);
        }
        else
        {
            // Narrow terminals: stack the main map above the minimap.
            var mainHeight = Math.Max(MinimumStackedMainHeight, mapHeight * 2 / 3);
            var miniHeight = Math.Max(MinimumStackedMinimapHeight, mapHeight - mainHeight);
            DrawMain(screen, environment, footprint, heatmap, level, mainCenter,
                mapX, mapY, mapWidth, mainHeight);
            DrawMinimap(screen, environment, footprint, heatmap, camera,
                overview.Level, level, mainCenter,
                mapWidth, mainHeight,
                mapX, mapY + mainHeight, mapWidth, miniHeight);
        }

        var legendY = mapY + mapHeight;
        screen.Text(mapX, legendY, "Penguins per hex",
            HeatmapPalette.DefaultColor, LegendTitleWidth);
        var paletteStart = mapX + LegendTitleWidth;
        for (var i = 0; i < LegendCounts.Length; i++)
        {
            var x = paletteStart + i * HexColumnSpacing;
            if (x >= mapX + mapWidth)
            {
                break;
            }

            screen.Put(x, legendY,
                FromPaint(HeatmapPalette.GetDensityPaint(LegendCounts[i])));
        }

        var (column, row) = footprint.ToOffset(camera.Center);
        var status = camera.Level.HasValue
            ? $"Zoom L{level}   ({column}, {row})"
            : $"Overview L{overview.Level}   ({column}, {row})";
        screen.Text(mapX, legendY + 1, status, HeatmapPalette.DefaultColor, mapWidth);
        screen.Text(OuterInset, height - OuterInset,
            "Arrows/WASD pan   Shift+arrows fine   +/- zoom   R reset   Q quit",
            HeatmapPalette.DefaultColor, width - 2 * OuterInset);

        return screen.ToAnsiRows(ansi);
    }

    /// <summary>
    /// Choose the first zoom level whose entire world fits within the minimap.
    /// This uses the same layout math as RenderRows so +/- navigation cannot
    /// disagree with the zoom level actually drawn.
    /// </summary>
    public static (int Level, HexRegion Region) FitOverview(
        HexHeatmap heatmap, int terminalWidth, int terminalHeight)
    {
        var width = Math.Max(0, terminalWidth - TerminalEdgeMargin);
        var height = Math.Max(0, terminalHeight - TerminalEdgeMargin);
        // 6 columns = two outer insets + two iteration-content insets.
        // 9 rows = iteration top + iteration bottom reserve + footer reserve
        // minus the overlap with the iteration content inset.
        var mapWidth = width - (2 * OuterInset + 2 * IterationContentInset);
        var mapHeight = height - (IterationPanelBottomReserve + IterationFooterReserve);
        return ChooseOverview(heatmap, mapWidth, mapHeight, width >= SideBySideWidth);
    }

    /// <summary>
    /// Arrow navigation moves by roughly one sixth of the visible map span,
    /// measured in display hex cells (half the terminal text-column count).
    /// </summary>
    public static int PanStride(int terminalWidth, int terminalHeight)
    {
        var width = Math.Max(MinimumWidth, terminalWidth - TerminalEdgeMargin);
        var height = Math.Max(MinimumHeight, terminalHeight - TerminalEdgeMargin);
        var mapWidth = width - (2 * OuterInset + 2 * IterationContentInset);
        var mapHeight = height - (IterationPanelBottomReserve + IterationFooterReserve);
        var mainWidth = width >= SideBySideWidth
            ? Math.Max(MinimumMainWidth, mapWidth * MainWidthPercent / PercentageBase)
            : mapWidth;
        var mainHeight = width >= SideBySideWidth
            ? mapHeight
            : Math.Max(MinimumStackedMainHeight, mapHeight * 2 / 3);
        return Math.Max(1, Math.Min(
            (mainWidth - 2 * IterationContentInset) / HexColumnSpacing,
            mainHeight - 2 * IterationContentInset) / PanStrideDivisor);
    }

    /// <summary>
    /// A displayed world hexagon of radius R spans 4R+1 columns and 2R+1
    /// rows on the terminal. Pick a radius that fits both panels, plus margin
    /// for world-border dots, then ask the hex heatmap for the right level.
    /// </summary>
    private static (int Level, HexRegion Region) ChooseOverview(
        HexHeatmap heatmap, int width, int height, bool sideBySide)
    {
        var miniWidth = sideBySide
            ? width - Math.Max(MinimumMainWidth,
                width * MainWidthPercent / PercentageBase) - PanelGap
            : width;
        var miniHeight = sideBySide
            ? height - Math.Clamp(height / InformationHeightDivisor,
                MinimumInformationHeight, MaximumInformationHeight)
            : Math.Max(MinimumStackedMinimapHeight,
                height - Math.Max(MinimumStackedMainHeight, height * 2 / 3));
        var availableWidth = Math.Max(1, Math.Min(width, miniWidth) - 2);
        var availableHeight = Math.Max(1, Math.Min(height, miniHeight) - 2);

        // A radius-R hex needs 4R+1 columns by 2R+1 rows.
        var radius = Math.Max(0,
            Math.Min((availableWidth - 3) / 4, (availableHeight - 3) / 2));
        return heatmap.FitWorld(radius);
    }

    /// <summary>
    /// The camera exposes a rectangular screen window. Converting the camera
    /// axial center (q,r) to screen coordinates (2q+r,r) leaves hex cell shape
    /// and population data unchanged.
    /// </summary>
    private static void DrawMain(
        Screen screen, HexEnvironment environment, HexFootprint footprint,
        HexHeatmap heatmap, int level, HexCell center,
        int x, int y, int width, int height)
    {
        var title = level == 0 ? "Hex grid · L0" : $"Heatmap · L{level}";
        screen.Box(x, y, width, height, title, PanelColor);

        var content = MapScreenBounds.Centered(
            2 * center.Q + center.R, center.R,
            Math.Max(1, width - 2), Math.Max(1, height - 2));
        DrawCells(screen, environment, footprint, heatmap, level, content,
            x + 1, y + 1, null, null);
    }

    /// <summary>
    /// Draw a fixed world overview. Its content never pans: only the yellow
    /// rectangle projected from the main window and its center marker move.
    /// Both viewports use the same unrotated hexagonal coordinate axes.
    /// </summary>
    private static void DrawMinimap(
        Screen screen, HexEnvironment environment, HexFootprint footprint,
        HexHeatmap heatmap, MapCamera camera,
        int overviewLevel, int detailLevel, HexCell mainCenter,
        int mainWidth, int mainHeight,
        int x, int y, int width, int height)
    {
        screen.Box(x, y, width, height, $"Minimap · L{overviewLevel}", PanelColor);
        var minimap = MapScreenBounds.Centered(
            0, 0, Math.Max(1, width - 2), Math.Max(1, height - 2));
        var mainVisible = MapScreenBounds.Centered(
            2 * mainCenter.Q + mainCenter.R, mainCenter.R,
            Math.Max(1, mainWidth - 2), Math.Max(1, mainHeight - 2));
        MinimapViewport? selection = camera.Level.HasValue
            ? ViewportProjection.Project(mainVisible, detailLevel, overviewLevel)
            : null;
        HexCell? selectedCenter = camera.Level.HasValue
            ? HexGridScale.ToLevel(camera.Center, overviewLevel)
            : null;

        DrawCells(screen, environment, footprint, heatmap, overviewLevel, minimap,
            x + 1, y + 1, selection, selectedCenter);
    }

    /// <summary>Show the camera's base-cell position and the minimap legend.</summary>
    private static void DrawInformation(
        Screen screen, HexFootprint footprint, MapCamera camera,
        int level, int overviewLevel,
        int x, int y, int width, int height)
    {
        screen.Box(x, y, width, height, "Information", PanelColor);
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

        for (var i = 0; i < lines.Length && i < height - 2; i++)
        {
            screen.Text(x + 1, y + i + 1, lines[i], HeatmapPalette.DefaultColor, width - 2);
        }
    }

    /// <summary>
    /// Rasterize only the requested rectangle. Hex cells occur on alternating
    /// screen columns: screenX = 2*q+r, screenY = r. The other columns remain
    /// blank so the hexagonal rows appear staggered in a monospace terminal.
    /// </summary>
    private static void DrawCells(
        Screen screen, HexEnvironment environment, HexFootprint footprint,
        HexHeatmap heatmap, int level, MapScreenBounds bounds,
        int screenX, int screenY,
        MinimapViewport? selection, HexCell? cameraCenter)
    {
        var world = level == 0 ? footprint.Region : heatmap.GetRegion(level);

        for (var y = 0; y < bounds.Height; y++)
        {
            var row = bounds.Top + y;

            for (var x = 0; x < bounds.Width; x++)
            {
                var mapX = bounds.Left + x;
                // Invert screenX = 2*q+r to recover q; odd values fall
                // between cell centers and must remain empty spacer columns.
                var axialColumn = mapX - row;
                if ((axialColumn & 1) != 0)
                {
                    continue;
                }

                var cell = new HexCell(axialColumn / 2, row);
                var paint = GetCellPaint(environment, footprint, heatmap, world, level, cell);

                if (cameraCenter is { } center && cell == center)
                {
                    paint = new Pixel('●', CenterColor);
                }
                else if (selection is { } selected && selected.IsOnBorder(mapX, row))
                {
                    paint = new Pixel('⬢', YellowColor);
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
    private static Pixel GetCellPaint(
        HexEnvironment environment, HexFootprint footprint, HexHeatmap heatmap,
        HexRegion world, int level, HexCell cell)
    {
        if (world.Contains(cell))
        {
            var population = level == 0
                ? BasePopulation(environment, footprint, cell)
                : heatmap.GetPopulation(level, cell);
            return FromPaint(HeatmapPalette.GetDensityPaint(population));
        }

        return cell.DistanceTo(world.Center) == world.Radius + 1
            ? FromPaint(HeatmapPalette.WorldEdgePaint)
            : FromPaint(HeatmapPalette.OutsidePaint);
    }

    private static int BasePopulation(
        HexEnvironment environment, HexFootprint footprint, HexCell cell)
    {
        var (column, row) = footprint.ToOffset(cell);
        return environment[column, row] == HexEnvironment.EmptyCell ? 0 : 1;
    }

    private static Pixel FromPaint(MapPaint paint) =>
        new(paint.Glyph, paint.Color);

    /// <summary>A glyph and 24-bit RGB foreground; no background colors.</summary>
    private readonly record struct Pixel(char Glyph, uint Foreground);

    /// <summary>
    /// A flat row-major array of terminal cells. Painting is random access
    /// (O(1)); all rendering is completed before terminal I/O begins.
    /// </summary>
    private sealed class Screen
    {
        private readonly Pixel[] _cells;
        private readonly int _width;
        private readonly int _height;

        public Screen(int width, int height)
        {
            _width = width;
            _height = height;
            _cells = new Pixel[width * height];
            Array.Fill(_cells, new Pixel(' ', HeatmapPalette.DefaultColor));
        }

        /// <summary>Clip draws against terminal size instead of world size.</summary>
        public void Put(int x, int y, Pixel value)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                _cells[y * _width + x] = value;
            }
        }

        private void Put(int x, int y, char glyph, uint color) =>
            Put(x, y, new Pixel(glyph, color));

        /// <summary>
        /// Write clipped text directly to cells. Control bytes are sanitized
        /// so a simulation name/path cannot issue terminal escape commands.
        /// </summary>
        public void Text(int x, int y, string value, uint color, int maximumLength)
        {
            if (maximumLength <= 0)
            {
                return;
            }

            var length = Math.Min(value.Length, maximumLength);
            for (var i = 0; i < length; i++)
            {
                var ch = value[i];
                // Embedded ANSI/control bytes must never escape into the terminal.
                Put(x + i, y, char.IsControl(ch) ? ' ' : ch, color);
            }
        }

        /// <summary>
        /// Draw the four corners and edges of one rectangular display panel.
        /// Box drawing is separate from the underlying hexagonal world.
        /// </summary>
        public void Box(int x, int y, int width, int height, string title, uint color)
        {
            if (width < 2 || height < 2)
            {
                return;
            }

            for (var i = 1; i < width - 1; i++)
            {
                Put(x + i, y, '─', color);
                Put(x + i, y + height - 1, '─', color);
            }

            for (var i = 1; i < height - 1; i++)
            {
                Put(x, y + i, '│', color);
                Put(x + width - 1, y + i, '│', color);
            }

            Put(x, y, '╭', color);
            Put(x + width - 1, y, '╮', color);
            Put(x, y + height - 1, '╰', color);
            Put(x + width - 1, y + height - 1, '╯', color);

            if (width > 6)
            {
                Text(x + 2, y, " " + title + " ", color, width - 4);
            }
        }

        /// <summary>
        /// Encode each completed screen row. Foreground escapes are emitted
        /// only when a cell's RGB color differs from the previous cell.
        /// Plain output omits escapes for redirected stdout.
        /// </summary>
        public string[] ToAnsiRows(bool ansi)
        {
            var result = new string[_height];

            for (var row = 0; row < _height; row++)
            {
                var builder = new StringBuilder(_width * 3);
                var currentColor = HeatmapPalette.DefaultColor;
                for (var col = 0; col < _width; col++)
                {
                    var cell = _cells[row * _width + col];
                    if (ansi && cell.Foreground != currentColor)
                    {
                        currentColor = cell.Foreground;
                        if (currentColor == HeatmapPalette.DefaultColor)
                        {
                            builder.Append(AnsiCodes.ResetStyle);
                        }
                        else
                        {
                            AnsiCodes.AppendForegroundRgb(builder, currentColor);
                        }
                    }

                    builder.Append(cell.Glyph);
                }

                // Keep every row independently styled for changed-row output.
                if (ansi) builder.Append(AnsiCodes.ResetStyle);
                result[row] = builder.ToString();
            }

            return result;
        }
    }
}
