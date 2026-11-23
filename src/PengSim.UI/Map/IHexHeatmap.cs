using PengSim.BL.Contracts;
using PengSim.BL.Model;

namespace PengSim.UI.Map;

/// <summary>Groups the population of a single frame into display buckets at multiple resolutions.</summary>
internal interface IHexHeatmap
{
    /// <summary>Recounts occupied cells for the supplied iteration.</summary>
    void Rebuild(IEnvironment environment);

    /// <summary>Gets the population in a bucket at the given aggregate zoom level.</summary>
    int GetPopulation(int zoom, HexCell cell);

    /// <summary>Gets the enclosing hex region at the given aggregate zoom level.</summary>
    HexRegion GetRegion(int zoom);

    /// <summary>Chooses the finest aggregate level whose world fits within the requested radius.</summary>
    (int Level, HexRegion Region) FitWorld(int maxRadius);
}
