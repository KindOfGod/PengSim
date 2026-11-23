namespace PengSim.UI.Demo;

internal interface IDemoScreen : IDisposable
{
    void Draw(string title, params (string Text, uint Color)[] lines);
    bool WaitFor(char shortcut, Action draw);
}
