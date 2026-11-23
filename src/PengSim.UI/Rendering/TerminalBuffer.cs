using System.Text;

namespace PengSim.UI.Rendering;

/// <summary>One styled terminal character (foreground color only).</summary>
internal readonly record struct TerminalPixel(char Glyph, uint Foreground)
{
    public const string ResetStyle = "\u001b[0m";
    private const int RedShift = 16;   // 0xRRGGBB: red occupies bits 16..23.
    private const int GreenShift = 8;  // Green occupies bits 8..15.
    private const uint ChannelMask = 0xFF; // A color channel is eight bits.

    /// <summary>
    /// SGR 38;2;R;G;Bm sets 24-bit RGB *foreground* color.
    /// For example, 38;2;255;215;95m is the yellow minimap outline.
    /// </summary>
    public void AppendForegroundRgb(StringBuilder builder)
    {
        builder.Append("\u001b[38;2;")
            .Append((Foreground >> RedShift) & ChannelMask).Append(';')
            .Append((Foreground >> GreenShift) & ChannelMask).Append(';')
            .Append(Foreground & ChannelMask).Append('m');
    }
}

/// <summary>
/// A flat row-major array of terminal cells. Painting is random access
/// (O(1)); all rendering is completed before terminal I/O begins.
/// </summary>
internal sealed class TerminalBuffer : ITerminalBuffer
{
    private readonly TerminalPixel[] _cells;
    private readonly int _width;
    private readonly int _height;

    public TerminalBuffer(int width, int height)
    {
        _width = width;
        _height = height;
        _cells = new TerminalPixel[width * height];
        Array.Fill(_cells, new TerminalPixel(' ', IHeatmapPalette.DefaultColor));
    }

    /// <summary>Clip draws against terminal size instead of world size.</summary>
    public void Put(int x, int y, TerminalPixel value)
    {
        if (x >= 0 && x < _width && y >= 0 && y < _height)
        {
            _cells[y * _width + x] = value;
        }
    }

    /// <summary>
    /// Write clipped text directly to cells. Control bytes are sanitized
    /// so a simulation name/path cannot issue terminal escape commands.
    /// </summary>
    public void Text(int x, int y, string value, uint color, int maximumLength)
    {
        if (maximumLength <= 0)
        {
            return;
        }

        var length = Math.Min(value.Length, maximumLength);
        for (var i = 0; i < length; i++)
        {
            var ch = value[i];
            // Embedded ANSI/control bytes must never escape into the terminal.
            Put(x + i, y, char.IsControl(ch) ? ' ' : ch, color);
        }
    }

    /// <summary>
    /// Draw the four corners and edges of one rectangular display panel.
    /// Box drawing is separate from the underlying hexagonal world.
    /// </summary>
    public void Box(int x, int y, int width, int height, string title, uint color)
    {
        if (width < 2 || height < 2)
        {
            return;
        }

        for (var i = 1; i < width - 1; i++)
        {
            Put(x + i, y, '─', color);
            Put(x + i, y + height - 1, '─', color);
        }

        for (var i = 1; i < height - 1; i++)
        {
            Put(x, y + i, '│', color);
            Put(x + width - 1, y + i, '│', color);
        }

        Put(x, y, '╭', color);
        Put(x + width - 1, y, '╮', color);
        Put(x, y + height - 1, '╰', color);
        Put(x + width - 1, y + height - 1, '╯', color);

        if (width > 6)
        {
            Text(x + 2, y, " " + title + " ", color, width - 4);
        }
    }

    /// <summary>
    /// Encode each completed screen row. Foreground escapes are emitted
    /// only when a cell's RGB color differs from the previous cell.
    /// Plain output omits escapes for redirected stdout.
    /// </summary>
    public string[] ToAnsiRows(bool ansi)
    {
        var result = new string[_height];

        for (var row = 0; row < _height; row++)
        {
            var builder = new StringBuilder(_width * 3);
            var currentColor = IHeatmapPalette.DefaultColor;
            for (var col = 0; col < _width; col++)
            {
                var cell = _cells[row * _width + col];
                if (ansi && cell.Foreground != currentColor)
                {
                    currentColor = cell.Foreground;
                    if (currentColor == IHeatmapPalette.DefaultColor)
                    {
                        builder.Append(TerminalPixel.ResetStyle);
                    }
                    else
                    {
                        cell.AppendForegroundRgb(builder);
                    }
                }

                builder.Append(cell.Glyph);
            }

            // Keep every row independently styled for changed-row output.
            if (ansi) builder.Append(TerminalPixel.ResetStyle);
            result[row] = builder.ToString();
        }

        return result;
    }

    private void Put(int x, int y, char glyph, uint color) =>
        Put(x, y, new TerminalPixel(glyph, color));
}
