namespace PengSim.BL.Model;

/// <summary>An axial coordinate in a base or aggregate hex lattice.</summary>
public readonly record struct HexCell(int Q, int R)
{
    public static HexCell FromOffset(int column, int row) => new(column - (row >> 1), row);

    /// <summary>Returns the number of hex steps between two cells.</summary>
    public int DistanceTo(HexCell other)
    {
        var q = Q - other.Q;
        var r = R - other.R;

        return Math.Max(Math.Max(Math.Abs(q), Math.Abs(r)), Math.Abs(q + r));
    }
}
