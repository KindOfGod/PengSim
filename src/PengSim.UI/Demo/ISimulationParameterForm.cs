using PengSim.BL.Contracts;

namespace PengSim.UI.Demo;

/// <summary>Collects simulation parameters from the user, one field per prompt.</summary>
internal interface ISimulationParameterForm
{
    /// <summary>
    /// Prompts for the parameters on <paramref name="screen"/>, pre-filled from
    /// <paramref name="defaults"/>, and assigns a random placement seed. Returns null
    /// when the user goes back with Escape. The result is not validated yet.
    /// </summary>
    ISimulationParameters? Read(IDemoScreen screen, ISimulationParameters defaults);
}
