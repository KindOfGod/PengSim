using PengSim.BL.Contracts;
using PengSim.UI.Map;

namespace PengSim.UI.Rendering;

internal interface IFrameRenderer
{
    string[] RenderRows(
        IEnvironment environment,
        HexFootprint footprint,
        IHexHeatmap heatmap,
        IMapCamera camera,
        string simulationName,
        int iteration,
        int lastIteration,
        int speed,
        FrameLayout layout,
        bool ansi = true);
}
