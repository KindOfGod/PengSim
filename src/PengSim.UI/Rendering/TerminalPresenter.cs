using System.Text;

namespace PengSim.UI.Rendering;

/// <summary>
/// Presents a completed frame using a single buffered terminal write.
/// Unchanged rows are skipped, so panning and zooming do not resend the full
/// screen. The presenter does not calculate hexes or decide layout.
/// </summary>
internal sealed class TerminalPresenter : ITerminalPresenter
{
    private const string Csi = "\u001b[";
    private const string HideCursor = Csi + "?25l";
    private const string ShowCursor = Csi + "?25h";
    private const string HomeCursor = Csi + "H";
    private const string ClearScreen = Csi + "2J";
    private const string EraseCurrentLine = Csi + "2K";

    // The rows already contain ANSI foreground colors. Comparing complete
    // strings detects both changed glyphs and changed colors.
    private string[]? _previous;
    private int _width;
    private int _height;

    /// <summary>Hide the cursor and clear the old terminal contents once.</summary>
    public void Start()
    {
        Console.Out.Write(HideCursor + ClearScreen + HomeCursor);
        Console.Out.Flush();
    }

    /// <summary>
    /// Compose a single ANSI command buffer covering just changed rows.
    /// Resizing clears the terminal and invalidates the old row comparison,
    /// since coordinates and panel borders have changed.
    /// </summary>
    public void Present(string[] rows, int width, int height)
    {
        var full = _previous is null || width != _width || height != _height;
        var buffer = new StringBuilder();

        if (full)
        {
            buffer.Append(ClearScreen);
        }

        var limit = Math.Min(height, Math.Max(rows.Length, _previous?.Length ?? 0));
        for (var row = 0; row < limit; row++)
        {
            if (!full && row < rows.Length && row < _previous!.Length &&
                rows[row] == _previous[row])
            {
                continue;
            }

            // VT cursor positions are 1-based, whereas our row arrays are
            // zero-based. Erase the old row so shorter text leaves no trail.
            AppendMoveToRowStart(buffer, row);
            buffer.Append(EraseCurrentLine);

            if (row < rows.Length)
            {
                buffer.Append(rows[row]);
            }
        }

        buffer.Append(TerminalPixel.ResetStyle);
        Console.Out.Write(buffer.ToString());
        Console.Out.Flush();
        _previous = rows;
        _width = width;
        _height = height;
    }

    /// <summary>Restore terminal styles and the visible cursor on exit.</summary>
    public void Dispose()
    {
        Console.Out.Write(TerminalPixel.ResetStyle + ShowCursor + "\n");
        Console.Out.Flush();
    }

    /// <summary>CSI row;1H moves to column 1 of the given zero-based row.</summary>
    private static void AppendMoveToRowStart(StringBuilder builder, int row)
    {
        builder.Append(Csi).Append(row + 1).Append(";1H");
    }
}
