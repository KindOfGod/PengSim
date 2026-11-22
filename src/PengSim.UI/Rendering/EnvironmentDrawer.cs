using System.Diagnostics;
using System.Globalization;
using PengSim.UI.Map;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Rendering;

/// <summary>
/// Owns the interactive loop, but performs no map painting itself.
/// Input changes the camera; FrameRenderer builds a complete terminal snapshot;
/// TerminalPresenter publishes its changed rows in one buffered write.
/// This separation avoids partial frames and per-hexagon terminal I/O.
/// </summary>
internal sealed class EnvironmentDrawer
{
    // Without input, yield the CPU for 8 ms. A smaller value would spin more
    // often; a much larger one would make keyboard navigation feel sluggish.
    private const int InputPollMilliseconds = 8;

    // Coalesce key-repeat bursts into one camera update and one redraw.
    // Bound the batch so holding a key cannot indefinitely starve rendering.
    private const int MaxKeysPerFrame = 32;

    // Fixed dimensions when output is redirected and no TTY size is available.
    private const int RedirectedOutputWidth = 101;
    private const int RedirectedOutputHeight = 35;

    private const string ProfileEnabledVariable = "PENGSIM_PROFILE";
    private const string ProfilePathVariable = "PENGSIM_PROFILE_PATH";
    private const string DefaultProfilePath = "pengsim-profile.log";

    /// <summary>
    /// Initializes the sparse zoom index once, then draws frames from the
    /// current environment. Profiling writes only after terminal rendering
    /// finishes, so it cannot interfere with cursor positioning.
    /// </summary>
    public void Show(
        HexEnvironment environment,
        HexFootprint footprint,
        string simulationName,
        string? outputPath = null,
        int iteration = 0)
    {
        var profile = new Profile(
            System.Environment.GetEnvironmentVariable(ProfileEnabledVariable) == "1");
        try
        {
            // Building the sparse population index is an initialization cost,
            // not part of the per-keypress frame budget.
            var started = Stopwatch.GetTimestamp();
            var heatmap = new HexHeatmap(environment, footprint);
            profile.Startup = Stopwatch.GetTimestamp() - started;
            var camera = new MapCamera(footprint);

            // Draw returns fully prepared rows; it never writes to the terminal.
            string[] Draw(int width, int height, bool ansi = true) =>
                FrameRenderer.RenderRows(environment, footprint, heatmap, camera,
                    simulationName, outputPath, iteration, width, height, ansi);

            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                // Non-interactive output is plain text (no cursor movement,
                // no escape sequences) with a deterministic display size.
                foreach (var row in Draw(
                    RedirectedOutputWidth, RedirectedOutputHeight, ansi: false))
                {
                    Console.WriteLine(row);
                }

                return;
            }

            // Cursor management and terminal diffing live in TerminalPresenter;
            // the map renderer is unaware of terminal output operations.
            using var renderer = new TerminalPresenter();
            renderer.Start();
            renderer.Present(Draw(Console.WindowWidth, Console.WindowHeight),
                Console.WindowWidth, Console.WindowHeight);

            // Width/height are part of the key so resizing invalidates the
            // old screen even when the camera has not moved.
            var last = (camera.Center, camera.Level, Console.WindowWidth, Console.WindowHeight);
            while (true)
            {
                // Apply all queued keys before asking for a new complete frame.
                // Only the final camera state of this batch becomes visible.
                var count = 0;
                while (count < MaxKeysPerFrame && Console.KeyAvailable)
                {
                    if (!Navigate(Console.ReadKey(intercept: true), camera, heatmap))
                    {
                        return;
                    }

                    count++;
                }

                var state = (camera.Center, camera.Level, Console.WindowWidth, Console.WindowHeight);
                if (state != last)
                {
                    // Measure rendering/composition separately from terminal
                    // output; previous profiling showed layout can dominate.
                    started = Stopwatch.GetTimestamp();
                    var rows = Draw(state.Item3, state.Item4);
                    var ready = Stopwatch.GetTimestamp();
                    renderer.Present(rows, state.Item3, state.Item4);
                    profile.Record(ready - started, Stopwatch.GetTimestamp() - ready);
                    last = state;
                }

                if (count == 0)
                {
                    Thread.Sleep(InputPollMilliseconds);
                }
            }
        }
        finally
        {
            profile.Save();
        }
    }

    /// <summary>
    /// Interpret one key. Normal pan moves several displayed hexes; Shift+pan
    /// moves one base hex. Zoom never alters the BL's playable world geometry.
    /// </summary>
    private static bool Navigate(ConsoleKeyInfo input, MapCamera camera, HexHeatmap heatmap)
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

        if (camera.Level is int level && (horizontal, vertical) != (0, 0))
        {
            if (input.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                camera.PanFine(horizontal, vertical);
            }
            else
            {
                camera.Pan(horizontal, vertical, level,
                    FrameRenderer.PanStride(Console.WindowWidth, Console.WindowHeight));
            }
        }

        if (input.KeyChar is '+' or '-')
        {
            // The overview level must come from the exact same layout
            // calculation used to render the minimap (including resize).
            var overview = FrameRenderer.FitOverview(
                heatmap, Console.WindowWidth, Console.WindowHeight).Level;
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

    /// <summary>
    /// Optional lightweight timing accumulator. A report is written after
    /// the screen has been restored, not during an interactive frame.
    /// </summary>
    private sealed class Profile(bool enabled)
    {
        private long _prepare;
        private long _output;
        private long _slowestOutput;
        private int _frames;

        public long Startup { get; set; }

        public void Record(long preparation, long output)
        {
            if (!enabled) return;
            _frames++;
            _prepare += preparation;
            _output += output;
            _slowestOutput = Math.Max(_slowestOutput, output);
        }

        public void Save()
        {
            if (!enabled) return;

            // Stopwatch timestamps are ticks; convert via the measured clock
            // frequency rather than assuming any fixed tick duration.
            var frequency = (double)Stopwatch.Frequency;
            var frames = Math.Max(1, _frames);
            string Ms(double ticks) => (ticks * 1000 / frequency).ToString("F2", CultureInfo.InvariantCulture);
            var content = string.Join("\n",
                "PengSim performance profile",
                "Renderer: Direct terminal compositor",
                $"Recorded frames: {_frames}",
                $"Initial sparse bucket index: {Ms(Startup)} ms",
                $"Average frame preparation: {Ms(_prepare / (double)frames)} ms",
                $"Average output update: {Ms(_output / (double)frames)} ms",
                $"Slowest output update: {Ms(_slowestOutput)} ms",
                "");

            var path = System.Environment.GetEnvironmentVariable(ProfilePathVariable);
            if (string.IsNullOrWhiteSpace(path)) path = DefaultProfilePath;
            try
            {
                File.WriteAllText(Path.GetFullPath(path), content);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Console.Error.WriteLine($"Could not save profile: {error.Message}");
            }
        }
    }
}
