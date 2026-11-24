using System.Text;
using PengSim.UI.Rendering;

namespace PengSim.UI.Demo;

internal sealed class DemoScreen : IDemoScreen
{
    private const int ProgressBarWidth = 22;
    private const int MaxPanelWidth = 54;
    private const int PanelPadding = 4;
    private const int MaxInputLength = 80;
    private const int InputPollMilliseconds = 32;
    private const uint Accent = IHeatmapPalette.Accent;
    private const uint Normal = IHeatmapPalette.DefaultColor;
    private const uint Muted = 0x949494;
    private const uint ErrorColor = 0xff5f5f;

    private readonly ITerminalPresenter _presenter;
    private readonly CreateTerminalBuffer _createBuffer;

    public DemoScreen(ITerminalPresenter presenter, CreateTerminalBuffer createBuffer)
    {
        _presenter = presenter;
        _createBuffer = createBuffer;
        _presenter.Start();
    }

    public MenuChoice? WaitForMenuChoice()
    {
        while (true)
        {
            var key = ReadKey(DrawMainMenu);
            if (key.Key == ConsoleKey.Q)
            {
                return null;
            }

            switch (key.KeyChar)
            {
                case '1':
                    return MenuChoice.FromParameters;
                case '2':
                    return MenuChoice.FromCode;
                case '3':
                    return MenuChoice.Demo;
            }
        }
    }

    public string? ReadLine(string title, string label, string initialText, string? error)
    {
        var text = new StringBuilder(initialText);
        var shownError = error;
        while (true)
        {
            var currentText = text.ToString();
            var currentError = shownError;
            var key = ReadKey(() => DrawInput(title, label, currentText, currentError));
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    return currentText;
                case ConsoleKey.Escape:
                    return null;
                case ConsoleKey.Backspace:
                    if (text.Length > 0)
                    {
                        text.Length--;
                    }
                    break;
                default:
                    if (!char.IsControl(key.KeyChar) && text.Length < MaxInputLength)
                    {
                        text.Append(key.KeyChar);
                    }
                    break;
            }

            // Hide the previous error once the user starts correcting the input.
            shownError = null;
        }
    }

    public void ShowError(string title, string message)
    {
        var lines = WrapText(message, MaxPanelWidth - PanelPadding)
            .Select(line => (line, ErrorColor))
            .Append(("", Normal))
            .Append(("Press any key to return to the menu", Muted))
            .ToArray();
        ReadKey(() => Draw(title, lines));
    }

    public RunMode? WaitForRunMode(string name)
    {
        while (true)
        {
            var key = ReadKey(() => Draw(name,
                ("[Enter] Run fast", Accent),
                ("[P]     Run with progress bar", Accent),
                ("", Normal),
                ("[Esc] Back", Muted)));
            if (key.Key == ConsoleKey.Enter)
            {
                return RunMode.Fast;
            }
            if (key.Key == ConsoleKey.P)
            {
                return RunMode.WithProgress;
            }
            if (key.Key == ConsoleKey.Escape)
            {
                return null;
            }
        }
    }

    public void ShowRunning()
    {
        Draw("Generating simulation", ("Calculating all steps…", Normal));
    }

    public void ShowProgress(DemoProgress progress)
    {
        DrawProgress(progress, "Generating simulation", "Calculating steps…");
    }

    public bool WaitForAnalysis(DemoProgress result)
    {
        return WaitFor('A', () => DrawProgress(result, "Simulation complete", "[A] Analyze    [Q] Quit"));
    }

    public void Dispose()
    {
        _presenter.Dispose();
    }

    private void DrawMainMenu()
    {
        Draw("Main menu",
            ("[1] Simulation from parameters", Accent),
            ("[2] Simulation from code", Accent),
            ("[3] Demo simulation", Accent),
            ("", Normal),
            ("[Q] Quit", Normal));
    }

    private void DrawInput(string title, string label, string text, string? error)
    {
        // Keep the end of long input visible, which is where the cursor is.
        var visibleWidth = MaxPanelWidth - PanelPadding - 3;
        var visibleText = text.Length > visibleWidth ? "…" + text[^(visibleWidth - 1)..] : text;
        Draw(title,
            (label, Normal),
            ($"> {visibleText}_", Accent),
            ("", Normal),
            (error ?? "", ErrorColor),
            ("[Enter] Confirm    [Esc] Back", Muted));
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
            ($"Map        side {progress.MapSide:N0}, {progress.MapCells:N0} cells", Normal),
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
        var panelWidth = Math.Min(MaxPanelWidth, width - 2);
        var panelHeight = lines.Length + 2;
        var x = (width - panelWidth) / 2;
        var y = Math.Max(1, (height - panelHeight) / 2);
        buffer.Box(x, y, panelWidth, panelHeight, title, Accent);
        for (var i = 0; i < lines.Length; i++)
        {
            buffer.Text(x + 2, y + 1 + i, lines[i].Text, lines[i].Color, panelWidth - PanelPadding);
        }
        _presenter.Present(buffer.ToAnsiRows(true), width, height);
    }

    private static bool WaitFor(char shortcut, Action draw)
    {
        while (true)
        {
            var key = ReadKey(draw);
            if (key.Key == ConsoleKey.Q)
            {
                return false;
            }
            if (char.ToUpperInvariant(key.KeyChar) == shortcut)
            {
                return true;
            }
        }
    }

    /// <summary>Draws once, redraws on terminal resize, and returns the next key press.</summary>
    private static ConsoleKeyInfo ReadKey(Action draw)
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
                return Console.ReadKey(intercept: true);
            }

            Thread.Sleep(InputPollMilliseconds);
        }
    }

    private static List<string> WrapText(string text, int width)
    {
        var lines = new List<string>();
        var line = new StringBuilder();
        foreach (var word in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                lines.Add(line.ToString());
                line.Clear();
            }
            if (line.Length > 0)
            {
                line.Append(' ');
            }
            line.Append(word);
        }
        if (line.Length > 0)
        {
            lines.Add(line.ToString());
        }

        return lines;
    }
}
