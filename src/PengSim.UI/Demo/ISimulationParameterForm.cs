using PengSim.BL.Contracts;

namespace PengSim.UI.Demo;

/// <summary>Collects simulation parameters from the user, one field per prompt.</summary>
internal interface ISimulationParameterForm
{
    /// <summary>
    /// Prompts for every parameter on <paramref name="screen"/>, pre-filled from
    /// <paramref name="defaults"/>. Returns null when the user goes back with Escape.
    /// The result is not validated yet.
    /// </summary>
    ISimulationParameters? Read(IDemoScreen screen, ISimulationParameters defaults);
}
