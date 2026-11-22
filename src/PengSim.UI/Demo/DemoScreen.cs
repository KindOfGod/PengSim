using PengSim.UI.Rendering;

namespace PengSim.UI.Demo;

internal sealed class DemoScreen : IDisposable
{
    private readonly TerminalPresenter _presenter = new();

    public DemoScreen() => _presenter.Start();

    public void Dispose() => _presenter.Dispose();

    public void Draw(string title, params (string Text, uint Color)[] lines)
    {
        var width = Console.WindowWidth;
        var height = Console.WindowHeight;
        var buffer = new TerminalBuffer(width, height);
        buffer.Box(0, 0, width, height, "Penguin Simulation", 0x949494);
        var panelWidth = Math.Min(54, width - 2);
        var panelHeight = lines.Length + 2;
        var x = (width - panelWidth) / 2;
        var y = Math.Max(1, (height - panelHeight) / 2);
        buffer.Box(x, y, panelWidth, panelHeight, title, HeatmapPalette.Accent);
        for (var i = 0; i < lines.Length; i++)
        {
            buffer.Text(x + 2, y + 1 + i, lines[i].Text, lines[i].Color, panelWidth - 4);
        }
        _presenter.Present(buffer.ToAnsiRows(true), width, height);
    }

    public static bool WaitFor(char shortcut, Action draw)
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
                if (key.Key == ConsoleKey.Q) return false;
                if (char.ToUpperInvariant(key.KeyChar) == shortcut) return true;
            }
            else Thread.Sleep(32);
        }
    }
}
