namespace PengSim.UI.Demo;

/// <summary>An entry the user can pick in the main menu.</summary>
internal enum MenuChoice
{
    /// <summary>Enter the simulation parameters one by one.</summary>
    FromParameters,

    /// <summary>Paste a code created by the parameter codec.</summary>
    FromCode,

    /// <summary>Run the built-in demo parameters.</summary>
    Demo,
}
