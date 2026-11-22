namespace PengSim.UI.Map;

/// <summary>Maps the centered playable hex into the simulation storage grid.</summary>
internal readonly record struct HexFootprint(int Radius)
{
    /// <summary>Gets the width and height of the square storage grid.</summary>
    public int Size => Radius * 2 + 1;

    /// <summary>Gets the playable hex centered on the axial origin.</summary>
    public HexRegion Region => new(default, Radius);

    /// <summary>Converts an offset-grid position to centered axial coordinates.</summary>
    public HexCell ToAxial(int column, int row) =>
        new(column - Radius - (row >> 1) + (Radius >> 1), row - Radius);

    /// <summary>Converts a centered axial cell back to an offset-grid position.</summary>
    public (int Column, int Row) ToOffset(HexCell cell)
    {
        var row = cell.R + Radius;

        return (cell.Q + Radius + (row >> 1) - (Radius >> 1), row);
    }

    /// <summary>Checks whether a storage-grid position belongs to the playable hex.</summary>
    public bool Contains(int column, int row) => Region.Contains(ToAxial(column, row));
}
