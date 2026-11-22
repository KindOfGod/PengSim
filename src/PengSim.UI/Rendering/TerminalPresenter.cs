using System.Text;

namespace PengSim.UI.Rendering;

/// <summary>
/// Presents a completed frame using a single buffered terminal write.
/// Unchanged rows are skipped, so panning and zooming do not resend the full
/// screen. The presenter does not calculate hexes or decide layout.
/// </summary>
internal sealed class TerminalPresenter : IDisposable
{
    // The rows already contain ANSI foreground colors. Comparing complete
    // strings detects both changed glyphs and changed colors.
    private string[]? _previous;
    private int _width;
    private int _height;

    /// <summary>Hide the cursor and clear the old terminal contents once.</summary>
    public void Start()
    {
        Console.Out.Write(AnsiCodes.HideCursor + AnsiCodes.ClearScreen + AnsiCodes.HomeCursor);
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
            buffer.Append(AnsiCodes.ClearScreen);
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
            AnsiCodes.AppendMoveToRowStart(buffer, row);
            buffer.Append(AnsiCodes.EraseCurrentLine);

            if (row < rows.Length)
            {
                buffer.Append(rows[row]);
            }
        }

        buffer.Append(AnsiCodes.ResetStyle);
        Console.Out.Write(buffer.ToString());
        Console.Out.Flush();
        _previous = rows;
        _width = width;
        _height = height;
    }

    /// <summary>Restore terminal styles and the visible cursor on exit.</summary>
    public void Dispose()
    {
        Console.Out.Write(AnsiCodes.ResetStyle + AnsiCodes.ShowCursor + "\n");
        Console.Out.Flush();
    }
}

/// <summary>
/// Named VT/ANSI escape sequences used by the terminal renderer.
///
/// ESC '[' is the Control Sequence Introducer (CSI). 'm' finishes an SGR
/// (Select Graphic Rendition) color/style command; 'H' positions the cursor;
/// 'K' erases a line; and 'J' erases the screen. This small helper keeps the
/// protocol-specific numbers out of layout and map drawing code.
/// </summary>
internal static class AnsiCodes
{
    private const string Csi = "\u001b[";
    private const int RedShift = 16;   // 0xRRGGBB: red occupies bits 16..23.
    private const int GreenShift = 8;  // Green occupies bits 8..15.
    private const uint ChannelMask = 0xFF; // A color channel is eight bits.

    public const string ResetStyle = Csi + "0m";
    public const string HideCursor = Csi + "?25l";
    public const string ShowCursor = Csi + "?25h";
    public const string HomeCursor = Csi + "H";
    public const string ClearScreen = Csi + "2J";
    public const string EraseCurrentLine = Csi + "2K";

    /// <summary>
    /// SGR 38;2;R;G;Bm sets 24-bit RGB *foreground* color.
    /// For example, 38;2;255;215;95m is the yellow minimap outline.
    /// </summary>
    public static void AppendForegroundRgb(StringBuilder builder, uint rgb)
    {
        builder.Append(Csi).Append("38;2;")
            .Append((rgb >> RedShift) & ChannelMask).Append(';')
            .Append((rgb >> GreenShift) & ChannelMask).Append(';')
            .Append(rgb & ChannelMask).Append('m');
    }

    /// <summary>CSI row;1H moves to column 1 of the given zero-based row.</summary>
    public static void AppendMoveToRowStart(StringBuilder builder, int row)
    {
        builder.Append(Csi).Append(row + 1).Append(";1H");
    }
}
