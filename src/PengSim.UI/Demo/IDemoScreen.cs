namespace PengSim.UI.Demo;

/// <summary>Displays demo menus and handles their keyboard shortcuts.</summary>
internal interface IDemoScreen : IDisposable
{
    /// <summary>Draws a titled panel with the supplied text and foreground colors.</summary>
    void Draw(string title, params (string Text, uint Color)[] lines);

    /// <summary>Redraws on resize and returns true for the requested shortcut or false for Quit.</summary>
    bool WaitFor(char shortcut, Action draw);
}
