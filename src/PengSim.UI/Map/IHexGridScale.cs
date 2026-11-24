
namespace PengSim.UI.Map;

/// <summary>Converts axial coordinates between the base grid and display levels.</summary>
internal interface IHexGridScale
{
    /// <summary>Maps a base cell to its bucket at the specified zoom level.</summary>
    HexCell ToLevel(HexCell cell, int level);

    /// <summary>Maps a display bucket to its center in base-grid coordinates.</summary>
    HexCell ToBase(HexCell cell, int level);
}
