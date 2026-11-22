using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI;

internal sealed class EnvironmentDrawer(IAnsiConsole console)
{
    private static readonly (int Maximum, string Dot, string Label)[] HeatmapScale =
    [
        (0, "[grey]·[/]", "0"),
        (1, "[blue]●[/]", "1"),
        (3, "[aqua]●[/]", "2–3"),
        (7, "[green]●[/]", "4–7"),
        (15, "[yellow]●[/]", "8–15"),
        (int.MaxValue, "[red]●[/]", "16+"),
    ];

    public void Show(HexEnvironment environment, IReadOnlyCollection<(int Column, int Row)> penguins)
    {
        var camera = new MapCamera(environment.Width, environment.Height);
        if (Console.IsInputRedirected || Console.IsOutputRedirected || !console.Profile.Capabilities.Interactive)
        {
            console.Write(Render(environment, penguins, camera, interactive: false));
            return;
        }

        console.Live(Render(environment, penguins, camera, interactive: true))
            .AutoClear(false)
            .Start(context =>
            {
                var lastBounds = camera.VisibleBounds;
                var lastSize = (console.Profile.Width, console.Profile.Height);
                context.Refresh();
                while (true)
                {
                    if (Console.KeyAvailable && !Navigate(Console.ReadKey(intercept: true), camera))
                    {
                        return;
                    }

                    var bounds = camera.VisibleBounds;
                    var size = (console.Profile.Width, console.Profile.Height);
                    if (bounds != lastBounds || size != lastSize)
                    {
                        context.UpdateTarget(Render(environment, penguins, camera, interactive: true));
                        context.Refresh();
                        lastBounds = bounds;
                        lastSize = size;
                    }

                    Thread.Sleep(30);
                }
            });
    }

    private IRenderable Render(
        HexEnvironment environment,
        IReadOnlyCollection<(int Column, int Row)> penguins,
        MapCamera camera,
        bool interactive)
    {
        int terminalWidth = console.Profile.Width;
        int terminalHeight = console.Profile.Height;
        bool showControls = interactive && terminalWidth >= 20 && terminalHeight >= 6;
        int width = Math.Max(1, terminalWidth - 4);
        int height = Math.Max(1, terminalHeight - (showControls ? 5 : 2));
        var bounds = camera.VisibleBounds;
        bool showHexes = camera.Zoom > 0 && bounds.Width <= width / 2 && bounds.Height <= height;
        bool showScale = !showHexes && terminalWidth >= HeatmapScale.Length && terminalHeight >= 4;
        string map = showHexes
            ? DrawHexes(environment, bounds)
            : DrawHeatmap(penguins, bounds, width, Math.Max(1, height - (showScale ? 1 : 0)));

        var panel = new Panel(new Markup(map))
            .Header(showHexes ? "Hex grid" : "Heatmap")
            .Border(BoxBorder.Rounded);

        var content = new List<IRenderable> { panel };
        if (showScale)
        {
            content.Add(new Markup(DrawScale(terminalWidth)));
        }

        if (showControls)
        {
            string controls = terminalWidth >= 42
                ? "Arrows/WASD pan  +/- zoom  R reset  Q quit"
                : "WASD +/- R Q";
            string status = $"x{Math.Pow(2, camera.Zoom):0}  {bounds.Width}×{bounds.Height}  ({bounds.Left},{bounds.Top})";
            content.Add(new Text(status));
            content.Add(new Text(controls));
        }

        return new Rows(content);
    }

    private static string DrawScale(int width)
    {
        string fullScale = "Penguins per bucket  "
            + string.Join("  ", HeatmapScale.Select(band => $"{band.Dot} {band.Label}"));
        if (width >= Markup.Remove(fullScale).Length)
        {
            return fullScale;
        }

        string dots = string.Concat(HeatmapScale.Select(band => band.Dot));
        return width >= 12 ? $"0 {dots} 16+" : dots;
    }

    private static string DrawHexes(HexEnvironment environment, MapViewport bounds)
    {
        var rows = new string[bounds.Height];
        for (int y = 0; y < bounds.Height; y++)
        {
            int row = bounds.Top + y;
            var cells = new string[bounds.Width];
            for (int x = 0; x < bounds.Width; x++)
            {
                cells[x] = environment[bounds.Left + x, row] == HexEnvironment.EmptyCell
                    ? "⬡"
                    : "[blue]⬢[/]";
            }

            rows[y] = (row % 2 == 0 ? "" : " ") + string.Join(" ", cells);
        }

        return string.Join("\n", rows);
    }

    private static string DrawHeatmap(
        IReadOnlyCollection<(int Column, int Row)> penguins,
        MapViewport bounds,
        int availableWidth,
        int availableHeight)
    {
        // Half-cell horizontal units preserve the odd-row offset when bucketing.
        int worldWidth = bounds.Width * 2;
        int columns = Math.Min(availableWidth, worldWidth);
        int rows = Math.Min(availableHeight, bounds.Height);
        var counts = new int[rows, columns];

        foreach (var (column, row) in penguins)
        {
            int x = column - bounds.Left;
            int y = row - bounds.Top;
            if (x >= 0 && x < bounds.Width && y >= 0 && y < bounds.Height)
            {
                int bucketX = (x * 2 + row % 2) * columns / worldWidth;
                int bucketY = y * rows / bounds.Height;
                counts[bucketY, bucketX]++;
            }
        }

        var markup = new StringBuilder();
        for (int y = 0; y < rows; y++)
        {
            if (y > 0)
            {
                markup.Append('\n');
            }

            for (int x = 0; x < columns; x++)
            {
                foreach (var band in HeatmapScale)
                {
                    if (counts[y, x] <= band.Maximum)
                    {
                        markup.Append(band.Dot);
                        break;
                    }
                }
            }
        }

        return markup.ToString();
    }

    private static bool Navigate(ConsoleKeyInfo input, MapCamera camera)
    {
        switch (input.Key)
        {
            case ConsoleKey.Q: return false;
            case ConsoleKey.LeftArrow or ConsoleKey.A: camera.Pan(-1, 0); break;
            case ConsoleKey.RightArrow or ConsoleKey.D: camera.Pan(1, 0); break;
            case ConsoleKey.UpArrow or ConsoleKey.W: camera.Pan(0, -1); break;
            case ConsoleKey.DownArrow or ConsoleKey.S: camera.Pan(0, 1); break;
            case ConsoleKey.R: camera.Reset(); break;
            default:
                if (input.KeyChar == '+')
                {
                    camera.ChangeZoom(1);
                }
                else if (input.KeyChar == '-')
                {
                    camera.ChangeZoom(-1);
                }

                break;
        }

        return true;
    }
}
