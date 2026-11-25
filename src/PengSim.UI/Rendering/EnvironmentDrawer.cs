using System.Diagnostics;
using PengSim.UI.Map;
using PengSim.BL.Contracts;

namespace PengSim.UI.Rendering;

/// <summary>
/// Owns the interactive loop, but performs no map painting itself.
/// Input changes the camera; FrameRenderer builds a complete terminal snapshot;
/// TerminalPresenter publishes its changed rows in one buffered write.
/// This separation avoids partial frames and per-hexagon terminal I/O.
/// </summary>
internal sealed class EnvironmentDrawer(
    IFrameRenderer frameRenderer,
    Func<HexFootprint, IHexHeatmap> createHeatmap,
    Func<HexFootprint, IMapCamera> createCamera,
    Func<ITerminalPresenter> createPresenter) : IEnvironmentDrawer
{
    // Bound the wait for keyboard input when paused or playing slowly.
    private const int InputPollMilliseconds = 8;

    // Coalesce key-repeat bursts into one camera update and one redraw.
    // Bound the batch so holding a key cannot indefinitely starve rendering.
    private const int MaxKeysPerFrame = 32;

    // Fixed dimensions when output is redirected and no TTY size is available.
    private const int RedirectedOutputWidth = 101;
    private const int RedirectedOutputHeight = 35;

    public void Show(IReadOnlyList<ISimulationStep> frames, HexFootprint footprint, string simulationName, string parameterCode)
    {
        var iteration = 0;
        var speed = 0;
        var nextFrame = TimeSpan.Zero;
        var clock = Stopwatch.StartNew();
        using IRenderProfile? profile = System.Environment.GetEnvironmentVariable("PENGSIM_PROFILE") == "1"
            ? new RenderProfile()
            : null;
        long Timestamp() => profile is null ? 0 : Stopwatch.GetTimestamp();

        var started = Timestamp();
        var heatmap = createHeatmap(footprint);
        heatmap.Rebuild(frames[iteration]);
        if (profile is not null) profile.Startup = Timestamp() - started;
        var camera = createCamera(footprint);

        void Draw(FrameLayout layout, ITerminalPresenter? presenter, int width, int height)
        {
            var preparing = Timestamp();
            var rows = frameRenderer.RenderRows(footprint, heatmap, camera,
                simulationName, parameterCode, iteration, frames.Count - 1, speed, layout, ansi: presenter is not null);
            var ready = Timestamp();
            if (presenter is null)
            {
                foreach (var row in rows) Console.WriteLine(row);
            }
            else
            {
                presenter.Present(rows, width, height);
            }
            profile?.Record(ready - preparing, Timestamp() - ready);
        }

        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            var layout = FrameLayout.Create(RedirectedOutputWidth, RedirectedOutputHeight);
            Draw(layout, null, RedirectedOutputWidth, RedirectedOutputHeight);
            return;
        }

        using var renderer = createPresenter();
        renderer.Start();
        var width = Console.WindowWidth;
        var height = Console.WindowHeight;
        Draw(FrameLayout.Create(width, height), renderer, width, height);
        var last = (camera.Center, camera.Level, width, height, iteration, speed);
        while (true)
        {
            width = Console.WindowWidth;
            height = Console.WindowHeight;
            var layout = FrameLayout.Create(width, height);
            var count = 0;
            while (count < MaxKeysPerFrame && Console.KeyAvailable)
            {
                var input = Console.ReadKey(intercept: true);
                if (input.Key == ConsoleKey.Backspace)
                {
                    iteration = 0;
                    speed = 0;
                }
                else if (input.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow)
                {
                    speed = 0;
                    iteration = Math.Clamp(iteration + (input.Key == ConsoleKey.LeftArrow ? -1 : 1),
                        0, frames.Count - 1);
                }
                else if (char.IsAsciiDigit(input.KeyChar))
                {
                    speed = input.KeyChar - '0';
                    if (speed > 0)
                    {
                        nextFrame = clock.Elapsed + TimeSpan.FromSeconds(1d / speed);
                    }
                }
                else if (!Navigate(input, camera, heatmap, layout))
                {
                    return;
                }

                count++;
            }

            var now = clock.Elapsed;
            if (speed > 0 && now >= nextFrame)
            {
                var interval = TimeSpan.FromSeconds(1d / speed);
                var due = 1 + (now - nextFrame).Ticks / interval.Ticks;
                var advance = (int)Math.Min(due, frames.Count - 1 - iteration);
                iteration += advance;
                nextFrame += interval * advance;
            }
            if (iteration == frames.Count - 1)
            {
                speed = 0;
            }

            var state = (camera.Center, camera.Level, width, height, iteration, speed);
            if (state != last)
            {
                if (iteration != last.iteration) heatmap.Rebuild(frames[iteration]);
                Draw(layout, renderer, width, height);
                last = state;
            }

            if (count == 0)
            {
                var wait = speed == 0 ? InputPollMilliseconds
                    : Math.Clamp((nextFrame - clock.Elapsed).TotalMilliseconds, 0, InputPollMilliseconds);
                Thread.Sleep((int)Math.Ceiling(wait));
            }
        }
    }

    /// <summary>
    /// Interpret one key. Normal pan moves several displayed hexes; Shift+pan
    /// moves one base hex. Zoom never alters the BL's playable world geometry.
    /// </summary>
    private static bool Navigate(
        ConsoleKeyInfo input, IMapCamera camera, IHexHeatmap heatmap, FrameLayout layout)
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
            ConsoleKey.A => (-1, 0),
            ConsoleKey.D => (1, 0),
            ConsoleKey.W => (0, -1),
            ConsoleKey.S => (0, 1),
            _ => (0, 0),
        };

        if (camera.Level is int level && (horizontal, vertical) != (0, 0))
        {
            if (input.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                camera.PanFine(horizontal, vertical);
            }
            else
            {
                camera.Pan(horizontal, vertical, level, layout.PanStride);
            }
        }

        if (input.KeyChar is '+' or '-')
        {
            // Navigation uses the same layout and overview as the frame.
            var overview = layout.FitOverview(heatmap).Level;
            if (input.KeyChar == '+')
            {
                camera.ZoomIn(overview);
            }
            else
            {
                camera.ZoomOut(overview);
            }
        }

        return true;
    }

}
