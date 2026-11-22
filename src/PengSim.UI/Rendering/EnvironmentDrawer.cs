using System.Text;
using PengSim.UI.Map;
using Spectre.Console;
using Spectre.Console.Rendering;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Rendering;

internal sealed class EnvironmentDrawer(IAnsiConsole console)
{
    private static readonly (int Maximum, string Color, string Label)[] HeatmapScale =
    [
        (0, "grey", "0"),
        (1, "blue", "1"),
        (3, "aqua", "2–3"),
        (7, "green", "4–7"),
        (15, "yellow", "8–15"),
        (int.MaxValue, "red", "16+"),
    ];

    public void Show(HexEnvironment environment, HexFootprint footprint, string simulationName, string? outputPath = null)
    {
        var heatmap = new HexHeatmap(environment, footprint);
        var camera = new MapCamera(footprint);
        var details = new Rows(
            new Text($"{Emoji.Known.Penguin}  Name: {simulationName}") { Overflow = Overflow.Ellipsis },
            new Text($"{Emoji.Known.FileFolder}  Path: {outputPath ?? "Unsaved"}") { Overflow = Overflow.Ellipsis });
        if (Console.IsInputRedirected || Console.IsOutputRedirected || !console.Profile.Capabilities.Interactive)
        {
            console.Write(Render(environment, footprint, heatmap, camera, details, interactive: false));
            return;
        }

        console.Clear();
        console.Live(Render(environment, footprint, heatmap, camera, details, interactive: true))
            .AutoClear(false)
            .Start(context =>
            {
                var lastCenter = camera.Center;
                int? lastLevel = camera.Level;
                var lastSize = (console.Profile.Width, console.Profile.Height);
                context.Refresh();
                while (true)
                {
                    if (Console.KeyAvailable && !Navigate(Console.ReadKey(intercept: true), camera, heatmap))
                    {
                        return;
                    }

                    var size = (console.Profile.Width, console.Profile.Height);
                    if (camera.Center != lastCenter || camera.Level != lastLevel || size != lastSize)
                    {
                        context.UpdateTarget(Render(environment, footprint, heatmap, camera, details, interactive: true));
                        context.Refresh();
                        lastCenter = camera.Center;
                        lastLevel = camera.Level;
                        lastSize = size;
                    }

                    Thread.Sleep(30);
                }
            });
    }

    private IRenderable Render(
        HexEnvironment environment,
        HexFootprint footprint,
        HexHeatmap heatmap,
        MapCamera camera,
        Rows details,
        bool interactive)
    {
        var layout = GetLayout(interactive);
        int detailRadius = Math.Max(0, layout.MainRadius - 1);
        var overview = FitOverview(heatmap, layout);
        int level = camera.Level ?? overview.Level;
        var view = camera.Level.HasValue
            ? new HexRegion(camera.Center.Ancestor(level), detailRadius)
            : overview.Region;
        var drawRegion = view with { Radius = layout.MainRadius };
        var counts = level > 0 ? heatmap.GetCounts(level) : null;
        var worldAtLevel = level > 0 ? heatmap.GetRegion(level) : footprint.Region;

        var main = DrawPanel(level == 0 ? "Hex grid · L0" : $"Heatmap · L{level}", drawRegion, layout.MainWidth, cell =>
        {
            int distance = cell.DistanceTo(view.Center);
            if (distance > view.Radius)
            {
                return distance == view.Radius + 1 ? "[fuchsia]·[/]" : " ";
            }

            if (counts is not null)
            {
                return worldAtLevel.Contains(cell) ? DensityGlyph(counts.GetValueOrDefault(cell)) : "[grey]·[/]";
            }

            if (!worldAtLevel.Contains(cell))
            {
                return "[grey]·[/]";
            }

            var (column, row) = footprint.ToOffset(cell);
            return environment[column, row] == HexEnvironment.EmptyCell ? "⬡" : "[blue]⬢[/]";
        });

        int largestMarker = MarkerRadius(detailRadius, overview.Level - 1, overview.Level, layout.MiniRadius);
        int margin = Math.Min(largestMarker, layout.MiniRadius - overview.Region.Radius);
        var miniCounts = heatmap.GetCounts(overview.Level);
        int markerRadius = MarkerRadius(detailRadius, level, overview.Level, margin);
        var marker = new HexRegion(camera.Center.Ancestor(overview.Level), markerRadius);
        var miniBounds = MapBounds.FromHex(overview.Region);
        if (camera.Level.HasValue)
        {
            miniBounds = miniBounds.Union(MapBounds.FromHex(marker));
        }

        var mini = DrawPanel($"Minimap · L{overview.Level}", miniBounds, layout.MiniWidth, cell =>
        {
            if (camera.Level.HasValue && marker.Contains(cell))
            {
                return DensityGlyph(miniCounts.GetValueOrDefault(cell), highlighted: true);
            }

            return overview.Region.Contains(cell) ? DensityGlyph(miniCounts.GetValueOrDefault(cell)) : " ";
        });

        int informationRows = Math.Max(1, layout.SideHeight - (miniBounds.Height + 2) - 2);
        string emptyInformation = string.Join("\n", Enumerable.Repeat(" ", informationRows));
        var information = new Panel(new Text(emptyInformation)) { Width = mini.Width }
            .Header("Information")
            .Border(BoxBorder.Rounded);
        var side = new Rows(mini, information);

        IRenderable maps;
        if (layout.SideBySide)
        {
            var grid = new Grid().AddColumns(
                new GridColumn { Width = main.Width, Padding = new Padding(0, 0) },
                new GridColumn { Width = 1, Padding = new Padding(0, 0) },
                new GridColumn { Width = mini.Width, Padding = new Padding(0, 0) });
            grid.AddRow(main, new Text(" "), side);
            maps = grid;
        }
        else
        {
            maps = new Rows(main, side);
        }

        var content = new List<IRenderable> { details, maps };
        int width = FrameContentWidth;
        if (layout.ShowScale)
        {
            content.Add(new Markup(DrawScale(width)));
        }

        if (layout.ShowControls)
        {
            var (column, row) = footprint.ToOffset(camera.Center);
            string position = camera.Level.HasValue ? $"Zoom L{level}  ({column},{row})" : $"Overview  ({column},{row})";
            if (camera.Level.HasValue && position.Length + 13 <= width)
            {
                position += "  ◈ viewport";
            }

            string controls = width >= 65
                ? "Arrows/WASD pan  Shift+arrows fine  +/- zoom  R reset  Q quit"
                : width >= 32 ? "Arrows  Shift: fine  +/-  R  Q" : "WASD +/- R Q";
            content.Add(new Text(position));
            content.Add(new Text(controls));
        }

        return new Panel(new Rows(content)) { Padding = new Padding(1, 0) }
            .Header("Penguine Simulation")
            .Border(BoxBorder.Rounded);
    }

    private static Panel DrawPanel(string title, HexRegion region, int maxWidth, Func<HexCell, string> glyph) =>
        DrawPanel(title, MapBounds.FromHex(region), maxWidth, cell => region.Contains(cell) ? glyph(cell) : " ");

    private static Panel DrawPanel(string title, MapBounds bounds, int maxWidth, Func<HexCell, string> glyph)
    {
        var text = new StringBuilder();
        for (int row = bounds.Top; row <= bounds.Bottom; row++)
        {
            if (row > bounds.Top)
            {
                text.Append('\n');
            }

            for (int x = bounds.Left; x <= bounds.Right; x++)
            {
                text.Append(((x - row) & 1) == 0 ? glyph(new HexCell((x - row) / 2, row)) : " ");
            }
        }

        int width = Math.Min(maxWidth, Math.Max(title.Length + 4, bounds.Width + 4));
        return new Panel(new Markup(text.ToString())) { Width = width }
            .Header(title)
            .Border(BoxBorder.Rounded);
    }

    private static string DensityGlyph(int count, bool highlighted = false)
    {
        string color = HeatmapScale.First(band => count <= band.Maximum).Color;
        return highlighted ? $"[{color} on white]◈[/]" : $"[{color}]{(count == 0 ? "⬡" : "⬢")}[/]";
    }

    private static string DrawScale(int width)
    {
        string fullScale = "Penguins per hex  "
            + string.Join("  ", HeatmapScale.Select(band => $"{DensityGlyph(band.Maximum)} {band.Label}"));
        if (width >= Markup.Remove(fullScale).Length)
        {
            return fullScale;
        }

        string glyphs = string.Concat(HeatmapScale.Select(band => DensityGlyph(band.Maximum)));
        return width >= 12 ? $"0 {glyphs} 16+" : glyphs;
    }

    private int FrameContentWidth => Math.Max(1, console.Profile.Width - 4);

    private Layout GetLayout(bool interactive)
    {
        int width = FrameContentWidth;
        int terminalHeight = console.Profile.Height > 0 ? console.Profile.Height : 24;
        // Reserve two border rows and two simulation details rows.
        int height = Math.Max(1, terminalHeight - 4);
        bool sideBySide = width >= 38;
        bool showControls = interactive && width >= 26 && height >= (sideBySide ? 9 : 14);
        bool showScale = width >= 6 && height >= (sideBySide ? 7 : 11);
        int mapHeight = height - (showControls ? 2 : 0) - (showScale ? 1 : 0);
        int miniWidth = sideBySide ? width * 48 / 100 : width;
        int sideHeight = sideBySide ? mapHeight : Math.Max(6, mapHeight / 2);
        int mainWidth = sideBySide ? width - miniWidth - 1 : width;
        int mainHeight = sideBySide ? mapHeight : mapHeight - sideHeight;

        return new Layout(mainWidth, FittingRadius(mainWidth, mainHeight),
            miniWidth, FittingRadius(miniWidth, sideHeight - 3), sideHeight, sideBySide, showControls, showScale);
    }

    private static int FittingRadius(int width, int height) =>
        Math.Max(0, Math.Min((width - 5) / 4, (height - 3) / 2));

    private static int MarkerRadius(int radius, int level, int miniLevel, int margin) =>
        Math.Min(margin, (int)Math.Round(radius * Math.Pow(7, (level - miniLevel) / 2.0)));

    private static (int Level, HexRegion Region) FitOverview(HexHeatmap heatmap, Layout layout) =>
        heatmap.FitWorld(Math.Min(Math.Max(0, layout.MainRadius - 1), layout.MiniRadius));

    private bool Navigate(ConsoleKeyInfo input, MapCamera camera, HexHeatmap heatmap)
    {
        if (input.Key == ConsoleKey.Q)
        {
            return false;
        }

        if (input.Key == ConsoleKey.R)
        {
            camera.Reset();
        }

        var direction = input.Key switch
        {
            ConsoleKey.LeftArrow or ConsoleKey.A => (-1, 0),
            ConsoleKey.RightArrow or ConsoleKey.D => (1, 0),
            ConsoleKey.UpArrow or ConsoleKey.W => (0, -1),
            ConsoleKey.DownArrow or ConsoleKey.S => (0, 1),
            _ => (0, 0),
        };
        var layout = GetLayout(interactive: true);
        if (camera.Level.HasValue && direction != (0, 0))
        {
            var overview = FitOverview(heatmap, layout);
            if (input.Modifiers.HasFlag(ConsoleModifiers.Shift) || overview.Region.Radius == 0)
            {
                camera.PanFine(direction.Item1, direction.Item2);
            }
            else
            {
                camera.Pan(direction.Item1, direction.Item2, overview.Level, heatmap);
            }
        }

        if (input.KeyChar is '+' or '-')
        {
            int overviewLevel = FitOverview(heatmap, layout).Level;
            if (input.KeyChar == '+')
            {
                camera.ZoomIn(overviewLevel);
            }
            else
            {
                camera.ZoomOut(overviewLevel);
            }
        }

        return true;
    }

    // The projected x coordinate uses half-column spacing between adjacent hex rows.
    private readonly record struct MapBounds(int Left, int Top, int Right, int Bottom)
    {
        public int Width => Right - Left + 1;
        public int Height => Bottom - Top + 1;

        public static MapBounds FromHex(HexRegion hex)
        {
            int x = 2 * hex.Center.Q + hex.Center.R;
            int r = hex.Radius;
            return new(x - 2 * r, hex.Center.R - r, x + 2 * r, hex.Center.R + r);
        }

        public MapBounds Union(MapBounds other) =>
            new(Math.Min(Left, other.Left), Math.Min(Top, other.Top),
                Math.Max(Right, other.Right), Math.Max(Bottom, other.Bottom));
    }

    private readonly record struct Layout(
        int MainWidth, int MainRadius, int MiniWidth, int MiniRadius, int SideHeight,
        bool SideBySide, bool ShowControls, bool ShowScale);
}
