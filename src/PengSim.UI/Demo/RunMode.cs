namespace PengSim.UI.Demo;

/// <summary>How the chosen simulation is calculated before the analysis view opens.</summary>
internal enum RunMode
{
    /// <summary>Calculate all steps in one call without updating the screen.</summary>
    Fast,

    /// <summary>Calculate one step at a time and show a progress bar.</summary>
    WithProgress,
}
