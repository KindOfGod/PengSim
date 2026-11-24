using PengSim.BL.Contracts;
using PengSim.BL.Model;

namespace PengSim.UI.Map;

/// <summary>
/// Translates the simulation's regular hex map into centered drawing coordinates,
/// and those into the square screen grid the camera moves on.
/// </summary>
/// <remarks>
/// Map coordinates have their origin at the bottom-left corner and R growing upward.
/// Drawing coordinates are centered on the map and R grows downward, so the
/// translation only shifts the origin and flips R.
/// </remarks>
internal readonly record struct HexFootprint
{
    private readonly PenguinPosition _mapCenter;
    private readonly (ushort A, ushort B, ushort C) _sides;

    /// <summary>Gets the distance from the center to each corner in cells.</summary>
    public int Radius { get; }

    /// <summary>Gets the width and height of the square screen grid around the hex.</summary>
    public int Size => 2 * Radius + 1;

    /// <summary>Gets the playable hex centered on the drawing origin.</summary>
    public HexRegion Region => new(default, Radius);

    private int ScreenCenter => Radius;

    public HexFootprint(IHexMap map)
    {
        if (map.SideA != map.SideB || map.SideB != map.SideC)
        {
            throw new ArgumentException("The renderer supports only hexes with three equal sides.", nameof(map));
        }

        _mapCenter = map.Center;
        _sides = (map.SideA, map.SideB, map.SideC);
        Radius = map.SideA - 1;
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

    /// <summary>Converts a position on the square screen grid to centered drawing coordinates.</summary>
    public HexCell ToAxial(int column, int row)
    {
        return new HexCell(column - ScreenCenter - (row >> 1) + (ScreenCenter >> 1), row - ScreenCenter);
    }

    /// <summary>Converts centered drawing coordinates back to a position on the square screen grid.</summary>
    public (int Column, int Row) ToOffset(HexCell cell)
    {
        var row = cell.R + ScreenCenter;

        return (cell.Q + ScreenCenter + (row >> 1) - (ScreenCenter >> 1), row);
    }
}
