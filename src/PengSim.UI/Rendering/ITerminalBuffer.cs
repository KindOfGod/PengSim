namespace PengSim.UI.Rendering;

internal delegate ITerminalBuffer CreateTerminalBuffer(int width, int height);

internal interface ITerminalBuffer
{
    void Put(int x, int y, TerminalPixel value);
    void Text(int x, int y, string value, uint color, int maximumLength);
    void Box(int x, int y, int width, int height, string title, uint color);
    string[] ToAnsiRows(bool ansi);
}
