using PengSim.BL.Contracts;
using PengSim.UI.Map;

namespace PengSim.UI.Rendering;

/// <summary>Presents a simulation history in the terminal.</summary>
internal interface IEnvironmentDrawer
{
    /// <summary>
    /// Opens playback at frame zero, or prints its plain overview when input or output is redirected.
    /// The header shows <paramref name="simulationName"/> and the <paramref name="parameterCode"/>
    /// that recreates the run.
    /// </summary>
    void Show(IReadOnlyList<ISimulationStep> frames, HexFootprint footprint, string simulationName, string parameterCode);
}
