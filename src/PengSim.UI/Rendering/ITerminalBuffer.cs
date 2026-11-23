namespace PengSim.UI.Rendering;

internal delegate ITerminalBuffer CreateTerminalBuffer(int width, int height);

/// <summary>Accumulates styled terminal cells before output.</summary>
internal interface ITerminalBuffer
{
    /// <summary>Paints a cell if its coordinates lie inside the buffer.</summary>
    void Put(int x, int y, TerminalPixel value);

    /// <summary>Draws text clipped to the maximum length and buffer bounds.</summary>
    void Text(int x, int y, string value, uint color, int maximumLength);

    /// <summary>Draws a rectangular border with the supplied title and color.</summary>
    void Box(int x, int y, int width, int height, string title, uint color);

    /// <summary>Encodes the buffer as rows, optionally including ANSI foreground colors.</summary>
    string[] ToAnsiRows(bool ansi);
}
