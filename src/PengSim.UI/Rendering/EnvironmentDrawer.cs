using System.Diagnostics;
using System.Globalization;
using PengSim.UI.Map;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Rendering;

/// <summary>Displays visible hexes with one terminal update per input batch.</summary>
internal sealed class EnvironmentDrawer
{
    private const int InputPollMilliseconds = 8;
    private const int MaxKeysPerFrame = 32;

    public void Show(
        HexEnvironment environment,
        HexFootprint footprint,
        string simulationName,
        string? outputPath = null,
        int iteration = 0)
    {
        var profile = new Profile(System.Environment.GetEnvironmentVariable("PENGSIM_PROFILE") == "1");
        try
        {
            var started = Stopwatch.GetTimestamp();
            var heatmap = new HexHeatmap(environment, footprint);
            profile.Startup = Stopwatch.GetTimestamp() - started;
            var camera = new MapCamera(footprint);

            string[] Draw(int width, int height, bool ansi = true) =>
                FrameRenderer.RenderRows(environment, footprint, heatmap, camera,
                    simulationName, outputPath, iteration, width, height, ansi);

            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                foreach (var row in Draw(101, 35, ansi: false))
                {
                    Console.WriteLine(row);
                }

                return;
            }

            using var renderer = new TerminalPresenter();
            renderer.Start();
            renderer.Present(Draw(Console.WindowWidth, Console.WindowHeight),
                Console.WindowWidth, Console.WindowHeight);

            var last = (camera.Center, camera.Level, Console.WindowWidth, Console.WindowHeight);
            while (true)
            {
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

    /// <summary>Optional post-exit performance summary.</summary>
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

            var path = System.Environment.GetEnvironmentVariable("PENGSIM_PROFILE_PATH");
            if (string.IsNullOrWhiteSpace(path)) path = "pengsim-profile.log";
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
