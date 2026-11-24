using PengSim.BL.Contracts;
using PengSim.BL.Model;

namespace PengSim.UI.Map;

/// <summary>
/// Translates the simulation's hex map into centered drawing coordinates and
/// describes the region the camera may move in.
/// </summary>
/// <remarks>
/// Map coordinates have their origin at the bottom-left corner and R growing upward.
/// Drawing coordinates are centered on <see cref="IHexMap.Center"/> and R grows downward,
/// so the translation only shifts the origin and flips R. The sides may have different lengths.
/// </remarks>
internal readonly record struct HexFootprint
{
    private readonly PenguinPosition _mapCenter;
    private readonly (ushort A, ushort B, ushort C) _sides;
    private readonly (int Left, int Right) _screenColumns;

    /// <summary>Gets the playable cells in drawing coordinates.</summary>
    public HexRegion Region { get; }

    /// <summary>Creates the footprint of <paramref name="map"/>.</summary>
    /// <remarks>
    /// The map's bounds translate directly: Q lies in [0, MaxQ], R in [0, MaxR], and the map's
    /// R - Q in [-(A - 1), B - 1]. Shifting by the center and flipping R gives the ranges below.
    /// </remarks>
    public HexFootprint(IHexMap map)
    {
        _mapCenter = map.Center;
        _sides = (map.SideA, map.SideB, map.SideC);

        var centerQ = map.Center.Q;
        var centerR = map.Center.R;
        var centerQMinusR = centerQ - centerR;
        Region = new HexRegion(
            MinQ: -centerQ,
            MaxQ: map.MaxQ - centerQ,
            MinR: centerR - map.MaxR,
            MaxR: centerR,
            MinS: centerQMinusR - (map.SideA - 1),
            MaxS: centerQMinusR + (map.SideB - 1));
        _screenColumns = Region.GetScreenColumns();
    }

    /// <summary>Checks whether a simulation map has the shape this footprint draws.</summary>
    public bool Matches(IHexMap map)
    {
        return (map.SideA, map.SideB, map.SideC) == _sides;
    }

    /// <summary>Converts a simulation map position to centered drawing coordinates.</summary>
    public HexCell FromMap(PenguinPosition position)
    {
        return new HexCell(position.Q - _mapCenter.Q, _mapCenter.R - position.R);
    }

    /// <summary>Converts centered drawing coordinates to simulation map coordinates, which may lie off the map.</summary>
    public (int Q, int R) ToMap(HexCell cell)
    {
        return (cell.Q + _mapCenter.Q, _mapCenter.R - cell.R);
    }

    /// <summary>
    /// Returns the cell closest to <paramref name="cell"/> inside the screen rectangle around
    /// <see cref="Region"/>. The rectangle's corners lie off the map but stay reachable.
    /// </summary>
    public HexCell ClampToBounds(HexCell cell)
    {
        var row = Math.Clamp(cell.R, Region.MinR, Region.MaxR);
        var column = Math.Clamp(2 * cell.Q + cell.R, _screenColumns.Left, _screenColumns.Right);
        var columnHasCell = ((column - row) & 1) == 0;
        if (!columnHasCell)
        {
            column += column > 0 ? -1 : 1;
        }

        return new HexCell((column - row) / 2, row);
    }
}
