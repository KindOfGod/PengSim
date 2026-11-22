using PengSim.UI.Map;
using Spectre.Console;
using Spectre.Console.Rendering;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Rendering;

/// <summary>Composes the simulation metadata, iteration view, and keyboard help.</summary>
internal static class SimulationFrameRenderer
{
    /// <summary>Formats the static name and output path for the session.</summary>
    public static Rows DrawDetails(string name, string? outputPath) =>
        new(
            new Markup($"[bold]{Emoji.Known.Penguin}  Name:[/] {Markup.Escape(name)}")
            {
                Overflow = Overflow.Ellipsis,
            },
            new Markup($"[bold]{Emoji.Known.FileFolder}  Path:[/] {Markup.Escape(outputPath ?? "Unsaved")}")
            {
                Overflow = Overflow.Ellipsis,
            });

    /// <summary>Draws the outer frame and its current iteration.</summary>
    public static Panel Draw(
        HexEnvironment environment,
        HexFootprint footprint,
        HexHeatmap heatmap,
        MapCamera camera,
        Rows details,
        int iteration,
        TerminalLayout layout)
    {
        var overview = layout.FitOverview(heatmap);
        var maps = MapPanels.Draw(environment, footprint, heatmap, camera, overview, layout);
        var iterationPanel = DrawIteration(maps, camera, footprint, iteration, layout);
        var content = new List<IRenderable> { details, iterationPanel };

        if (layout.ShowControls)
        {
            content.Add(new Text(GetControlText(layout.ContentWidth)));
        }

        return new Panel(new Rows(content))
        {
            Padding = new Padding(1, 0),
            BorderStyle = HeatmapPalette.FrameBorder,
        }
            .Header($"[bold {HeatmapPalette.Accent}]Penguine Simulation[/]")
            .Border(BoxBorder.Rounded);
    }

    /// <summary>Wraps the maps, legend, and camera position in the active iteration frame.</summary>
    private static Panel DrawIteration(
        IRenderable maps, MapCamera camera, HexFootprint footprint, int iteration, TerminalLayout layout)
    {
        var content = new List<IRenderable> { maps };

        if (layout.ShowScale)
        {
            content.Add(new Markup(HeatmapPalette.DrawScale(layout.ContentWidth)));
        }

        if (layout.ShowControls)
        {
            content.Add(new Text(GetPositionText(camera, footprint, layout.ContentWidth)));
        }

        return new Panel(new Rows(content))
        {
            Padding = new Padding(0, 0),
            BorderStyle = HeatmapPalette.ActiveBorder,
        }
            .Header($"Iteration {iteration}")
            .Border(BoxBorder.Rounded);
    }

    /// <summary>Formats the camera position and adds the marker key when it fits.</summary>
    private static string GetPositionText(MapCamera camera, HexFootprint footprint, int width)
    {
        var (column, row) = footprint.ToOffset(camera.Center);
        var position = camera.Level.HasValue
            ? $"Zoom L{camera.Level}  ({column},{row})"
            : $"Overview  ({column},{row})";

        if (camera.Level.HasValue && position.Length + 13 <= width)
        {
            position += "  ◈ viewport";
        }

        return position;
    }

    /// <summary>Chooses the keyboard help that fits the available width.</summary>
    private static string GetControlText(int width) => width switch
    {
        >= 65 => "Arrows/WASD pan  Shift+arrows fine  +/- zoom  R reset  Q quit",
        >= 32 => "Arrows  Shift: fine  +/-  R  Q",
        _ => "WASD +/- R Q",
    };
}
