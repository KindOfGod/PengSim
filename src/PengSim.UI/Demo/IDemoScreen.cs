namespace PengSim.UI.Demo;

/// <summary>Displays the demo menus and generation progress.</summary>
internal interface IDemoScreen : IDisposable
{
    /// <summary>Shows the main menu and returns true when the user chooses Run demo.</summary>
    bool WaitForDemoStart();

    /// <summary>Displays the latest generation progress and timings.</summary>
    void ShowProgress(DemoProgress progress);

    /// <summary>Shows the completed run and returns true when the user chooses Analyze.</summary>
    bool WaitForAnalysis(DemoProgress result);
}
