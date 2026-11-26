namespace PengSim.UI.Session;

/// <summary>Displays the menus, text prompts, and generation progress.</summary>
internal interface ISessionScreen : IDisposable
{
    /// <summary>Shows the main menu and returns the chosen entry, or null when the user quits.</summary>
    MenuChoice? WaitForMenuChoice();

    /// <summary>
    /// Asks for one line of text, starting with <paramref name="initialText"/>. Shows
    /// <paramref name="error"/> below the input when it is set. Returns the confirmed
    /// text, or null when the user presses Escape.
    /// </summary>
    string? ReadLine(string title, string label, string initialText, string? error);

    /// <summary>Shows an error message and waits until the user presses any key.</summary>
    void ShowError(string title, string message);

    /// <summary>
    /// Asks how to calculate the simulation named <paramref name="name"/>. Returns the
    /// chosen mode, or null when the user presses Escape.
    /// </summary>
    RunMode? WaitForRunMode(string name);

    /// <summary>Shows that the simulation is being calculated without progress updates.</summary>
    void ShowRunning();

    /// <summary>Displays the latest generation progress and timings.</summary>
    void ShowProgress(RunProgress progress);

    /// <summary>Shows the completed run, returning true on Enter or A and false on Q.</summary>
    bool WaitForAnalysis(RunProgress result);
}
