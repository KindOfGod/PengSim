using PengSim.BL.Contracts;
using PengSim.UI.Map;

namespace PengSim.UI.Rendering;

/// <summary>Presents a simulation history in the terminal.</summary>
internal interface IEnvironmentDrawer
{
    /// <summary>Opens playback at frame zero, or prints its plain overview when input or output is redirected.</summary>
    void Show(IReadOnlyList<IEnvironment> frames, HexFootprint footprint, string simulationName);
}
