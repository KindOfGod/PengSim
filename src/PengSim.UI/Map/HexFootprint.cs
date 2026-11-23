using PengSim.BL.Contracts;
using HexCell = PengSim.BL.Model.HexCell;
namespace PengSim.UI.Map;

/// <summary>Maps the centered playable hex into the simulation storage grid.</summary>
internal readonly record struct HexFootprint
{
    /// <summary>Gets the width and height of the square storage grid.</summary>
    public ushort Size { get; }

    // A regular hex spans an odd number of cells. Leave one padding row and column on even boards.
    public int Radius => (Size - 1) / 2;
    private int Center => Size / 2;

    /// <summary>Gets the playable hex centered on the axial origin.</summary>
    public HexRegion Region => new(default, Radius);

    public HexFootprint(ushort size)
    {
        ArgumentOutOfRangeException.ThrowIfZero(size);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, ISimulationEngine.MaxDimension);
        Size = size;
    }

    /// <summary>Converts an offset-grid position to centered axial coordinates.</summary>
    public HexCell ToAxial(int column, int row)
    {
        return new HexCell(column - Center - (row >> 1) + (Center >> 1), row - Center);
    }

    /// <summary>Converts a centered axial cell back to an offset-grid position.</summary>
    public (int Column, int Row) ToOffset(HexCell cell)
    {
        var row = cell.R + Center;

        return (cell.Q + Center + (row >> 1) - (Center >> 1), row);
    }

    /// <summary>Checks whether a storage-grid position belongs to the playable hex.</summary>
    public bool Contains(int column, int row)
    {
        return Region.Contains(ToAxial(column, row));
    }
}
