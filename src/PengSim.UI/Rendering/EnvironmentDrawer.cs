using PengSim.UI.Map;
using Spectre.Console;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Rendering;

/// <summary>Runs a console session for one simulation snapshot.</summary>
internal sealed class EnvironmentDrawer(IAnsiConsole console)
{
    private const int InputPollMilliseconds = 30;

    /// <summary>Shows the live map in a terminal or prints one frame when redirected.</summary>
    public void Show(
        HexEnvironment environment,
        HexFootprint footprint,
        string simulationName,
        string? outputPath = null,
        int iteration = 0)
    {
        var heatmap = new HexHeatmap(environment, footprint);
        var camera = new MapCamera(footprint);
        var details = SimulationFrameRenderer.DrawDetails(simulationName, outputPath);
        Func<bool, Panel> drawFrame = interactive => SimulationFrameRenderer.Draw(
            environment, footprint, heatmap, camera, details, iteration, GetLayout(interactive));

        if (Console.IsInputRedirected || Console.IsOutputRedirected || !console.Profile.Capabilities.Interactive)
        {
            console.Write(drawFrame(false));
            return;
        }

        console.Clear();
        console.Live(drawFrame(true))
            .AutoClear(false)
            .Start(context => RunLive(context, camera, heatmap, () => drawFrame(true)));
    }

    /// <summary>Refreshes the frame when navigation or a terminal resize changes it.</summary>
    private void RunLive(LiveDisplayContext context, MapCamera camera, HexHeatmap heatmap, Func<Panel> drawFrame)
    {
        var lastState = GetDisplayState(camera);
        context.Refresh();

        while (true)
        {
            if (Console.KeyAvailable && !Navigate(Console.ReadKey(intercept: true), camera, heatmap))
            {
                return;
            }

            var state = GetDisplayState(camera);
            if (state != lastState)
            {
                context.UpdateTarget(drawFrame());
                context.Refresh();
                lastState = state;
            }

            Thread.Sleep(InputPollMilliseconds);
        }
    }

    /// <summary>Captures the camera and terminal values that determine the rendered frame.</summary>
    private (HexCell Center, int? Level, int Width, int Height) GetDisplayState(MapCamera camera) =>
        (camera.Center, camera.Level, console.Profile.Width, console.Profile.Height);

    /// <summary>Fits the layout to the current console profile.</summary>
    private TerminalLayout GetLayout(bool interactive) =>
        TerminalLayout.Fit(console.Profile.Width, console.Profile.Height, interactive);

    /// <summary>Applies a keyboard command and returns whether the session should continue.</summary>
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

        var (horizontal, vertical) = input.Key switch
        {
            ConsoleKey.LeftArrow or ConsoleKey.A => (-1, 0),
            ConsoleKey.RightArrow or ConsoleKey.D => (1, 0),
            ConsoleKey.UpArrow or ConsoleKey.W => (0, -1),
            ConsoleKey.DownArrow or ConsoleKey.S => (0, 1),
            _ => (0, 0),
        };

        if (camera.Level.HasValue && (horizontal, vertical) != (0, 0))
        {
            var overview = GetLayout(interactive: true).FitOverview(heatmap);

            // A one-cell minimap has no neighbors, so it uses base-cell steps.
            if (input.Modifiers.HasFlag(ConsoleModifiers.Shift) || overview.Region.Radius == 0)
            {
                camera.PanFine(horizontal, vertical);
            }
            else
            {
                camera.Pan(horizontal, vertical, overview.Level, heatmap);
            }
        }

        if (input.KeyChar is '+' or '-')
        {
            var overviewLevel = GetLayout(interactive: true).FitOverview(heatmap).Level;
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
}
