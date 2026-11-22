using System.Text;
using PengSim.UI.Map;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Rendering;

/// <summary>
/// Direct, linear-time interactive renderer. No Spectre panel layout, markup
/// parsing, or segment splitting occurs during navigation. Only the visible
/// hexagons are sampled and written into a rectangular terminal buffer.
/// </summary>
internal static class FrameRenderer
{
    private const uint OuterColor = 0x949494;
    private const uint PanelColor = 0x767676;
    private const uint AccentColor = HeatmapPalette.Accent;
    private const uint YellowColor = 0xffd75f;
    private const uint CenterColor = 0xfff0a0;
    private static readonly int[] LegendCounts = [0, 1, 2, 4, 7, 15, 31, 63, 127, 128];

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
        var width = Math.Max(0, terminalWidth - 1);
        var height = Math.Max(0, terminalHeight - 1);
        if (width < 24 || height < 14)
        {
            return ["Terminal too small for map view. Resize to at least 25 x 15."];
        }

        var screen = new Screen(width, height);
        screen.Box(0, 0, width, height, "Penguin Simulation", OuterColor);
        screen.Text(2, 1, "Name: " + simulationName, HeatmapPalette.DefaultColor, width - 4);
        screen.Text(2, 2, "Path: " + (outputPath ?? "Unsaved"), HeatmapPalette.DefaultColor, width - 4);

        var iterationX = 2;
        var iterationY = 3;
        var iterationWidth = width - 4;
        var iterationHeight = height - 5;
        screen.Box(iterationX, iterationY, iterationWidth, iterationHeight,
            $"Iteration {iteration}", AccentColor);

        var mapX = iterationX + 1;
        var mapY = iterationY + 1;
        var mapWidth = iterationWidth - 2;
        var mapHeight = iterationHeight - 4; // reserve legend and position
        var overview = FitOverview(heatmap, terminalWidth, terminalHeight);
        var level = camera.Level ?? overview.Level;
        var mainCenter = camera.Level.HasValue
            ? HexGridScale.ToLevel(camera.Center, level)
            : overview.Region.Center;

        if (width >= 62)
        {
            var mainWidth = Math.Max(16, mapWidth * 52 / 100);
            var miniWidth = mapWidth - mainWidth - 1;
            var infoHeight = Math.Clamp(mapHeight / 5, 5, 8);
            var miniHeight = mapHeight - infoHeight;
            var miniX = mapX + mainWidth + 1;

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
            var mainHeight = Math.Max(5, mapHeight * 2 / 3);
            var miniHeight = Math.Max(3, mapHeight - mainHeight);
            DrawMain(screen, environment, footprint, heatmap, level, mainCenter,
                mapX, mapY, mapWidth, mainHeight);
            DrawMinimap(screen, environment, footprint, heatmap, camera,
                overview.Level, level, mainCenter,
                mapWidth, mainHeight,
                mapX, mapY + mainHeight, mapWidth, miniHeight);
        }

        var legendY = mapY + mapHeight;
        screen.Text(mapX, legendY, "Penguins per hex", HeatmapPalette.DefaultColor, 18);
        var paletteStart = mapX + 18;
        for (var i = 0; i < LegendCounts.Length; i++)
        {
            var x = paletteStart + i * 2;
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
        screen.Text(2, height - 2,
            "Arrows/WASD pan   Shift+arrows fine   +/- zoom   R reset   Q quit",
            HeatmapPalette.DefaultColor, width - 4);

        return screen.ToAnsiRows(ansi);
    }

    public static (int Level, HexRegion Region) FitOverview(
        HexHeatmap heatmap, int terminalWidth, int terminalHeight)
    {
        var width = Math.Max(0, terminalWidth - 1);
        var height = Math.Max(0, terminalHeight - 1);
        return ChooseOverview(heatmap, width - 6, height - 9, width >= 62);
    }

    public static int PanStride(int terminalWidth, int terminalHeight)
    {
        var width = Math.Max(24, terminalWidth - 1);
        var height = Math.Max(14, terminalHeight - 1);
        var mapWidth = width - 6;
        var mapHeight = height - 9;
        var mainWidth = width >= 62 ? Math.Max(16, mapWidth * 52 / 100) : mapWidth;
        var mainHeight = width >= 62 ? mapHeight : Math.Max(5, mapHeight * 2 / 3);
        return Math.Max(1, Math.Min((mainWidth - 2) / 2, mainHeight - 2) / 6);
    }

    private static (int Level, HexRegion Region) ChooseOverview(
        HexHeatmap heatmap, int width, int height, bool sideBySide)
    {
        var miniWidth = sideBySide
            ? width - Math.Max(16, width * 52 / 100) - 1
            : width;
        var miniHeight = sideBySide
            ? height - Math.Clamp(height / 5, 5, 8)
            : Math.Max(3, height - Math.Max(5, height * 2 / 3));
        var availableWidth = Math.Max(1, Math.Min(width, miniWidth) - 2);
        var availableHeight = Math.Max(1, Math.Min(height, miniHeight) - 2);

        // A radius-R hex needs 4R+1 columns by 2R+1 rows.
        var radius = Math.Max(0,
            Math.Min((availableWidth - 3) / 4, (availableHeight - 3) / 2));
        return heatmap.FitWorld(radius);
    }

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

    private readonly record struct Pixel(char Glyph, uint Foreground);

    /// <summary>Fixed-size screen with one terminal-cell value per position.</summary>
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

        public void Put(int x, int y, Pixel value)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                _cells[y * _width + x] = value;
            }
        }

        private void Put(int x, int y, char glyph, uint color) =>
            Put(x, y, new Pixel(glyph, color));

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
                            builder.Append("\u001b[0m");
                        }
                        else
                        {
                            builder.Append("\u001b[38;2;")
                                .Append((currentColor >> 16) & 255).Append(';')
                                .Append((currentColor >> 8) & 255).Append(';')
                                .Append(currentColor & 255).Append('m');
                        }
                    }

                    builder.Append(cell.Glyph);
                }

                if (ansi) builder.Append("\u001b[0m");
                result[row] = builder.ToString();
            }

            return result;
        }
    }
}
