
namespace PengSim.UI.Map;

/// <summary>Converts axial coordinates between the base grid and display levels.</summary>
internal interface IHexGridScale
{
    /// <summary>
    /// Maps a base cell to the nearest bucket at the specified zoom level.
    /// Equally near buckets on different rings favor the ring closer to the map center.
    /// </summary>
    HexCell ToLevel(HexCell cell, int level);

    /// <summary>Maps a display bucket to its center in base-grid coordinates.</summary>
    HexCell ToBase(HexCell cell, int level);
}
