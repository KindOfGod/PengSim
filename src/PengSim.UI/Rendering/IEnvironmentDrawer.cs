using PengSim.BL.Contracts;
using PengSim.UI.Map;

namespace PengSim.UI.Rendering;

internal interface IEnvironmentDrawer
{
    void Show(IReadOnlyList<IEnvironment> frames, HexFootprint footprint, string simulationName);
}
