namespace PengSim.UI.Rendering;

/// <summary>Manages terminal output for one interactive session.</summary>
internal interface ITerminalPresenter : IDisposable
{
    /// <summary>Clears the screen and hides the cursor for interactive rendering.</summary>
    void Start();

    /// <summary>Writes changed rows, refreshing the whole display when its dimensions change.</summary>
    void Present(string[] rows, int width, int height);
}
