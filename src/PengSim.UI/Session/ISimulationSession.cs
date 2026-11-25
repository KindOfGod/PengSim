namespace PengSim.UI.Session;

/// <summary>Runs one terminal session: choose a simulation, calculate it, and analyze it.</summary>
internal interface ISimulationSession
{
    /// <summary>
    /// Shows the main menu, calculates the chosen simulation, and opens its analysis view.
    /// When input or output is redirected, runs the demo and prints a plain overview instead.
    /// </summary>
    void Run();
}
