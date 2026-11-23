using PengSim.BL.Model;

namespace PengSim.UI.Map;

internal interface IMapCamera
{
    HexCell Center { get; }
    int? Level { get; }
    void Pan(int horizontal, int vertical, int level, int steps);
    void PanFine(int horizontal, int vertical);
    void ZoomIn(int overviewLevel);
    void ZoomOut(int overviewLevel);
    void Reset();
}
