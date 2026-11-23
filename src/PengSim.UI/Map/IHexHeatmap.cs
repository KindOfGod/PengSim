using PengSim.BL.Contracts;
using PengSim.BL.Model;

namespace PengSim.UI.Map;

internal interface IHexHeatmap
{
    void Rebuild(IEnvironment environment);
    int GetPopulation(int zoom, HexCell cell);
    HexRegion GetRegion(int zoom);
    (int Level, HexRegion Region) FitWorld(int maxRadius);
}
