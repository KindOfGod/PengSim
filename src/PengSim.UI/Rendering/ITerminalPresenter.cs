namespace PengSim.UI.Rendering;

internal interface ITerminalPresenter : IDisposable
{
    void Start();
    void Present(string[] rows, int width, int height);
}
