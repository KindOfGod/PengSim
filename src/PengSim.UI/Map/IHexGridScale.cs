using PengSim.BL.Model;

namespace PengSim.UI.Map;

internal interface IHexGridScale
{
    HexCell ToLevel(HexCell cell, int level);
    HexCell ToBase(HexCell cell, int level);
}
