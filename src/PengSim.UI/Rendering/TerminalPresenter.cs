using System.Text;

namespace PengSim.UI.Rendering;

/// <summary>Flushes only changed terminal rows in one buffered output.</summary>
internal sealed class TerminalPresenter : IDisposable
{
    private string[]? _previous;
    private int _width;
    private int _height;

    public void Start()
    {
        Console.Out.Write("\u001b[?25l\u001b[2J\u001b[H");
        Console.Out.Flush();
    }

    public void Present(string[] rows, int width, int height)
    {
        var full = _previous is null || width != _width || height != _height;
        var buffer = new StringBuilder();

        if (full)
        {
            buffer.Append("\u001b[2J");
        }

        var limit = Math.Min(height, Math.Max(rows.Length, _previous?.Length ?? 0));
        for (var row = 0; row < limit; row++)
        {
            if (!full && row < rows.Length && row < _previous!.Length &&
                rows[row] == _previous[row])
            {
                continue;
            }

            buffer.Append("\u001b[").Append(row + 1)
                .Append(";1H\u001b[2K");

            if (row < rows.Length)
            {
                buffer.Append(rows[row]);
            }
        }

        buffer.Append("\u001b[0m");
        Console.Out.Write(buffer.ToString());
        Console.Out.Flush();
        _previous = rows;
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        Console.Out.Write("\u001b[0m\u001b[?25h\n");
        Console.Out.Flush();
    }
}
