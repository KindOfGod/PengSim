using PengSim.UI.Map;

namespace PengSim.UI.Rendering;

/// <summary>Composes map, legend, and playback information into terminal rows.</summary>
internal interface IFrameRenderer
{
    /// <summary>
    /// Renders the selected iteration and camera view with optional ANSI styling.
    /// The header shows the simulation name and the parameter code that recreates the run.
    /// </summary>
    string[] RenderRows(
        HexFootprint footprint,
        IHexHeatmap heatmap,
        IMapCamera camera,
        string simulationName,
        string parameterCode,
        int iteration,
        int lastIteration,
        int speed,
        FrameLayout layout,
        bool ansi = true);
}
