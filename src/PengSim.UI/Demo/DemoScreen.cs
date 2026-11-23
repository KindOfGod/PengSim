using PengSim.UI.Rendering;

namespace PengSim.UI.Demo;

internal sealed class DemoScreen : IDemoScreen
{
    private const int ProgressBarWidth = 22;
    private const uint Accent = IHeatmapPalette.Accent;
    private const uint Normal = IHeatmapPalette.DefaultColor;
    private const uint Muted = 0x949494;

    private readonly ITerminalPresenter _presenter;
    private readonly CreateTerminalBuffer _createBuffer;

    public DemoScreen(ITerminalPresenter presenter, CreateTerminalBuffer createBuffer)
    {
        _presenter = presenter;
        _createBuffer = createBuffer;
        _presenter.Start();
    }

    public bool WaitForDemoStart()
    {
        return WaitFor('2', DrawMainMenu);
    }

    public void ShowProgress(DemoProgress progress)
    {
        DrawProgress(progress, "Generating demo", "Calculating steps…");
    }

    public bool WaitForAnalysis(DemoProgress result)
    {
        return WaitFor('A', () => DrawProgress(result, "Demo complete", "[A] Analyze    [Q] Quit"));
    }

    public void Dispose()
    {
        _presenter.Dispose();
    }

    private void DrawMainMenu()
    {
        Draw("Main menu",
            ("Create simulation (coming soon)", Muted),
            ("[2] Run demo", Accent),
            ("", Normal),
            ("[Q] Quit", Normal));
    }

    private void DrawProgress(DemoProgress progress, string title, string footer)
    {
        var filled = ProgressBarWidth * progress.CompletedSteps / progress.TotalSteps;
        var bar = new string('█', filled) + new string('░', ProgressBarWidth - filled);
        var percent = 100 * progress.CompletedSteps / progress.TotalSteps;
        var speed = progress.CompletedSteps / progress.IterationTime.TotalSeconds;
        Draw(title,
            ($"{bar}  {percent}%", Accent),
            ($"Steps      {progress.CompletedSteps:N0} / {progress.TotalSteps:N0}", Normal),
            ($"Elapsed    {progress.TotalTime.TotalSeconds:F3} s", Normal),
            ($"Speed      {speed:N1} steps/s", Normal),
            ("", Normal),
            ($"Penguins   {progress.Penguins:N0}", Normal),
            ($"Map        Radius {progress.Radius} · {progress.PlayableCells:N0} hexes", Normal),
            ($"Frames     {progress.CompletedSteps + 1:N0} in memory", Normal),
            ("", Normal),
            (footer, Accent));
    }

    private void Draw(string title, params (string Text, uint Color)[] lines)
    {
        var width = Console.WindowWidth;
        var height = Console.WindowHeight;
        var buffer = _createBuffer(width, height);
        buffer.Box(0, 0, width, height, "Penguin Simulation", Muted);
        var panelWidth = Math.Min(54, width - 2);
        var panelHeight = lines.Length + 2;
        var x = (width - panelWidth) / 2;
        var y = Math.Max(1, (height - panelHeight) / 2);
        buffer.Box(x, y, panelWidth, panelHeight, title, Accent);
        for (var i = 0; i < lines.Length; i++)
        {
            buffer.Text(x + 2, y + 1 + i, lines[i].Text, lines[i].Color, panelWidth - 4);
        }
        _presenter.Present(buffer.ToAnsiRows(true), width, height);
    }

    private static bool WaitFor(char shortcut, Action draw)
    {
        (int, int)? previousSize = null;
        while (true)
        {
            var size = (Console.WindowWidth, Console.WindowHeight);
            if (size != previousSize)
            {
                draw();
                previousSize = size;
            }
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Q)
                {
                    return false;
                }
                if (char.ToUpperInvariant(key.KeyChar) == shortcut)
                {
                    return true;
                }
            }
            else
            {
                Thread.Sleep(32);
            }
        }
    }
}
