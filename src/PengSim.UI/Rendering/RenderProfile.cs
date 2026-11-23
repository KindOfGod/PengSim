using System.Diagnostics;
using System.Globalization;

namespace PengSim.UI.Rendering;

internal sealed class RenderProfile : IRenderProfile
{
    private long _prepare;
    private long _output;
    private long _slowestOutput;
    private int _frames;

    public long Startup { get; set; }

    public void Record(long preparation, long output)
    {
        _frames++;
        _prepare += preparation;
        _output += output;
        _slowestOutput = Math.Max(_slowestOutput, output);
    }

    public void Dispose()
    {
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
