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

    public void Show(HexEnvironment environment, HexFootprint footprint)
    {
        var heatmap = new HexHeatmap(environment, footprint);
        var camera = new MapCamera(footprint);
        if (Console.IsInputRedirected || Console.IsOutputRedirected || !console.Profile.Capabilities.Interactive)
        {
            console.Write(Render(environment, footprint, heatmap, camera, interactive: false));
            return;
        }

        console.Clear();
        console.Live(Render(environment, footprint, heatmap, camera, interactive: true))
            .AutoClear(false)
            .Start(context =>
            {
                var lastCenter = camera.Center;
                int? lastLevel = camera.Level;
                var lastSize = (console.Profile.Width, console.Profile.Height);
                context.Refresh();
                while (true)
                {
                    if (Console.KeyAvailable && !Navigate(Console.ReadKey(intercept: true), camera, heatmap, footprint.Radius))
                    {
                        return;
                    }

                    var size = (console.Profile.Width, console.Profile.Height);
                    if (camera.Center != lastCenter || camera.Level != lastLevel || size != lastSize)
                    {
                        context.UpdateTarget(Render(environment, footprint, heatmap, camera, interactive: true));
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
        bool interactive)
    {
        var layout = GetLayout(interactive);
        int detailRadius = Math.Max(0, layout.MainRadius - 1);
        var overview = heatmap.FitWorld(detailRadius);
        int level = camera.Level ?? overview.Level;
        var view = camera.Level.HasValue
            ? new HexRegion(camera.Center.Ancestor(level), detailRadius)
            : overview.Region;
        var drawRegion = view with { Radius = layout.MainRadius };
        var counts = level > 0 ? heatmap.GetCounts(level) : null;

        var main = DrawPanel(level == 0 ? "Hex grid · L0" : $"Heatmap · L{level}", drawRegion, layout.MainWidth, cell =>
        {
            int distance = cell.DistanceTo(view.Center);
            if (distance > view.Radius)
            {
                return distance == view.Radius + 1 ? "[fuchsia]·[/]" : " ";
            }

            if (counts is not null)
            {
                return counts.TryGetValue(cell, out int count) ? DensityGlyph(count) : "[grey]·[/]";
            }

            if (!footprint.Region.Contains(cell))
            {
                return "[grey]·[/]";
            }

            var (column, row) = footprint.ToOffset(cell);
            return environment[column, row] == HexEnvironment.EmptyCell ? "⬡" : "[blue]⬢[/]";
        });

        var minimap = FitMinimap(heatmap, layout, footprint.Radius);
        var miniCounts = heatmap.GetCounts(minimap.Level);
        int markerRadius = MarkerRadius(detailRadius, level, minimap.Level, minimap.Margin);
        var marker = new HexRegion(camera.Center.Ancestor(minimap.Level), markerRadius);
        var miniBounds = minimap.Region with { Radius = minimap.Region.Radius + minimap.Margin };
        var mini = DrawPanel($"Minimap · L{minimap.Level}", miniBounds, layout.MiniWidth, cell =>
        {
            if (camera.Level.HasValue && marker.Contains(cell))
            {
                return DensityGlyph(miniCounts.GetValueOrDefault(cell), highlighted: true);
            }

            return minimap.Region.Contains(cell) ? DensityGlyph(miniCounts.GetValueOrDefault(cell)) : " ";
        });

        IRenderable maps;
        if (layout.SideBySide)
        {
            var grid = new Grid().AddColumns(
                new GridColumn { Width = main.Width, Padding = new Padding(0, 0) },
                new GridColumn { Width = 1, Padding = new Padding(0, 0) },
                new GridColumn { Width = mini.Width, Padding = new Padding(0, 0) });
            grid.AddRow(main, new Text(" "), mini);
            maps = grid;
        }
        else
        {
            maps = new Rows(main, mini);
        }

        var content = new List<IRenderable> { maps };
        if (layout.ShowScale)
        {
            content.Add(new Markup(DrawScale(console.Profile.Width)));
        }

        if (layout.ShowControls)
        {
            var (column, row) = footprint.ToOffset(camera.Center);
            string position = camera.Level.HasValue ? $"Zoom L{level}  ({column},{row})" : $"Overview  ({column},{row})";
            if (camera.Level.HasValue && position.Length + 13 <= console.Profile.Width)
            {
                position += "  ◈ viewport";
            }

            string controls = console.Profile.Width >= 65
                ? "Arrows/WASD pan  Shift+arrows fine  +/- zoom  R reset  Q quit"
                : console.Profile.Width >= 32 ? "Arrows  Shift: fine  +/-  R  Q" : "WASD +/- R Q";
            content.Add(new Text(position));
            content.Add(new Text(controls));
        }

        return new Rows(content);
    }

    private static Panel DrawPanel(string title, HexRegion region, int maxWidth, Func<HexCell, string> glyph)
    {
        var text = new StringBuilder();
        for (int r = -region.Radius; r <= region.Radius; r++)
        {
            if (r > -region.Radius)
            {
                text.Append('\n');
            }

            text.Append(' ', Math.Abs(r));
            int firstQ = Math.Max(-region.Radius, -r - region.Radius);
            int lastQ = Math.Min(region.Radius, -r + region.Radius);
            for (int q = firstQ; q <= lastQ; q++)
            {
                if (q > firstQ)
                {
                    text.Append(' ');
                }

                text.Append(glyph(new HexCell(region.Center.Q + q, region.Center.R + r)));
            }
        }

        int width = Math.Min(maxWidth, Math.Max(title.Length + 4, 4 * region.Radius + 5));
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

    private Layout GetLayout(bool interactive)
    {
        int width = console.Profile.Width;
        int height = console.Profile.Height > 0 ? console.Profile.Height : 24;
        bool sideBySide = width >= 38;
        bool showControls = interactive && width >= 26 && height >= (sideBySide ? 7 : 11);
        bool showScale = width >= 6 && height >= (sideBySide ? 5 : 9);
        int mapHeight = height - (showControls ? 2 : 0) - (showScale ? 1 : 0);
        int miniWidth = sideBySide ? Math.Clamp(width / 2 - 3, 15, 37) : width;
        int miniHeight = sideBySide ? mapHeight : Math.Max(3, mapHeight / 3);
        int mainWidth = sideBySide ? width - miniWidth - 1 : width;
        int mainHeight = sideBySide ? mapHeight : mapHeight - miniHeight;

        return new Layout(mainWidth, FittingRadius(mainWidth, mainHeight),
            miniWidth, FittingRadius(miniWidth, miniHeight), sideBySide, showControls, showScale);
    }

    private static int FittingRadius(int width, int height) =>
        Math.Max(0, Math.Min((width - 5) / 4, (height - 3) / 2));

    private static int MarkerRadius(int radius, int level, int miniLevel, int margin) =>
        Math.Min(margin, (int)Math.Round(radius * Math.Pow(7, (level - miniLevel) / 2.0)));

    private static (int Level, HexRegion Region, int Margin) FitMinimap(HexHeatmap heatmap, Layout layout, int worldRadius)
    {
        int detailRadius = Math.Max(0, layout.MainRadius - 1);
        int firstZoomLevel = heatmap.FitWorld(detailRadius).Level - 1;
        double largestViewRadius = Math.Min(worldRadius, detailRadius * Math.Pow(7, firstZoomLevel / 2.0));
        int availableRadius = layout.MiniRadius;
        while (true)
        {
            var overview = heatmap.FitWorld(availableRadius);
            int margin = (int)Math.Round(largestViewRadius / Math.Pow(7, overview.Level / 2.0));
            if (overview.Region.Radius + margin <= layout.MiniRadius || overview.Region.Radius == 0)
            {
                return (overview.Level, overview.Region, Math.Min(margin, layout.MiniRadius - overview.Region.Radius));
            }

            availableRadius = overview.Region.Radius - 1;
        }
    }

    private bool Navigate(ConsoleKeyInfo input, MapCamera camera, HexHeatmap heatmap, int worldRadius)
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
            var overview = FitMinimap(heatmap, layout, worldRadius);
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
            int overviewLevel = heatmap.FitWorld(Math.Max(0, layout.MainRadius - 1)).Level;
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

    private readonly record struct Layout(
        int MainWidth, int MainRadius, int MiniWidth, int MiniRadius,
        bool SideBySide, bool ShowControls, bool ShowScale);
}
