using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI;

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

        console.Live(Render(environment, footprint, heatmap, camera, interactive: true))
            .AutoClear(false)
            .Start(context =>
            {
                var lastView = camera.View;
                int lastZoom = camera.Zoom;
                var lastSize = (console.Profile.Width, console.Profile.Height);
                context.Refresh();
                while (true)
                {
                    if (Console.KeyAvailable && !Navigate(Console.ReadKey(intercept: true), camera))
                    {
                        return;
                    }

                    var view = camera.View;
                    var size = (console.Profile.Width, console.Profile.Height);
                    if (view != lastView || camera.Zoom != lastZoom || size != lastSize)
                    {
                        context.UpdateTarget(Render(environment, footprint, heatmap, camera, interactive: true));
                        context.Refresh();
                        lastView = view;
                        lastZoom = camera.Zoom;
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
        var layout = GetLayout(camera.Zoom, interactive);
        var view = camera.View;
        bool showDetail = camera.Zoom > 0 && view.Radius <= layout.MainRadius;
        Panel main;
        if (showDetail)
        {
            main = DrawPanel("Hex grid · L0", view, layout.MainWidth, cell =>
            {
                if (!footprint.Region.Contains(cell))
                {
                    return "[grey]·[/]";
                }

                var (column, row) = footprint.ToOffset(cell);
                return environment[column, row] == HexEnvironment.EmptyCell ? "⬡" : "[blue]⬢[/]";
            });
        }
        else
        {
            var map = heatmap.GetView(view, layout.MainRadius);
            main = DrawPanel($"Heatmap · L{map.Level}", map.Region, layout.MainWidth,
                cell => map.Counts.TryGetValue(cell, out int count) ? DensityGlyph(count) : "[grey]·[/]");
        }

        IRenderable maps = main;
        if (camera.Zoom > 0)
        {
            var overview = heatmap.GetView(footprint.Region, layout.MiniRadius);
            var highlighted = heatmap.ProjectViewport(view, overview.Level);
            var mini = DrawPanel($"Minimap · L{overview.Level}", overview.Region, layout.MiniWidth,
                cell => DensityGlyph(overview.Counts.GetValueOrDefault(cell), highlighted.Contains(cell)));

            if (layout.SideBySide)
            {
                var grid = new Grid().AddColumns(
                    new GridColumn { Width = layout.MainWidth, Padding = new Padding(0, 0) },
                    new GridColumn { Width = 1, Padding = new Padding(0, 0) },
                    new GridColumn { Width = layout.MiniWidth, Padding = new Padding(0, 0) });
                grid.AddRow(main, new Text(" "), mini);
                maps = grid;
            }
            else
            {
                maps = new Rows(main, mini);
            }
        }

        var content = new List<IRenderable> { maps };
        if (layout.ShowScale)
        {
            content.Add(new Markup(DrawScale(console.Profile.Width)));
        }

        if (layout.ShowControls)
        {
            var (column, row) = footprint.ToOffset(view.Center);
            string position = $"x{Math.Pow(2, camera.Zoom):0}  radius {view.Radius}  ({column},{row})";
            if (camera.Zoom > 0 && position.Length + 13 <= console.Profile.Width)
            {
                position += "  ◈ viewport";
            }

            string controls = console.Profile.Width >= 42
                ? "Arrows/WASD pan  +/- zoom  R reset  Q quit"
                : "WASD +/- R Q";
            content.Add(new Text(position));
            content.Add(new Text(controls));
        }

        return new Rows(content);
    }

    private static Panel DrawPanel(string title, HexRegion region, int width, Func<HexCell, string> glyph)
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

    private Layout GetLayout(int zoom, bool interactive)
    {
        int width = console.Profile.Width;
        int height = console.Profile.Height > 0 ? console.Profile.Height : 24;
        bool showMini = zoom > 0;
        bool sideBySide = showMini && width >= 38;
        bool showControls = interactive && width >= 26 && height >= (showMini && !sideBySide ? 11 : 7);
        bool showScale = width >= 6 && height >= (showMini && !sideBySide ? 9 : 5);
        int mapHeight = height - (showControls ? 2 : 0) - (showScale ? 1 : 0);
        int miniWidth = sideBySide ? Math.Clamp(width / 3, 15, 27) : width;
        int miniHeight = !showMini || sideBySide ? mapHeight : Math.Max(3, mapHeight / 3);
        int mainWidth = sideBySide ? width - miniWidth - 1 : width;
        int mainHeight = showMini && !sideBySide ? mapHeight - miniHeight : mapHeight;

        return new Layout(mainWidth, FittingRadius(mainWidth, mainHeight),
            miniWidth, FittingRadius(miniWidth, miniHeight), sideBySide, showControls, showScale);
    }

    private static int FittingRadius(int width, int height) =>
        Math.Max(0, Math.Min((width - 5) / 4, (height - 3) / 2));

    private bool Navigate(ConsoleKeyInfo input, MapCamera camera)
    {
        switch (input.Key)
        {
            case ConsoleKey.Q: return false;
            case ConsoleKey.LeftArrow or ConsoleKey.A: camera.Pan(-1, 0); break;
            case ConsoleKey.RightArrow or ConsoleKey.D: camera.Pan(1, 0); break;
            case ConsoleKey.UpArrow or ConsoleKey.W: camera.Pan(0, -1); break;
            case ConsoleKey.DownArrow or ConsoleKey.S: camera.Pan(0, 1); break;
            case ConsoleKey.R: camera.Reset(); break;
        }

        if (input.KeyChar == '+' && (camera.Zoom == 0 || camera.View.Radius > GetLayout(camera.Zoom, true).MainRadius))
        {
            camera.ChangeZoom(1);
        }
        else if (input.KeyChar == '-')
        {
            camera.ChangeZoom(-1);
        }

        return true;
    }

    private readonly record struct Layout(
        int MainWidth, int MainRadius, int MiniWidth, int MiniRadius,
        bool SideBySide, bool ShowControls, bool ShowScale);
}
