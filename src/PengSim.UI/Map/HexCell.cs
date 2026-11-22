namespace PengSim.UI.Map;

/// <summary>An axial coordinate in a base or aggregate hex lattice.</summary>
internal readonly record struct HexCell(int Q, int R)
{
    /// <summary>Returns the number of hex steps between two cells on the same level.</summary>
    public int DistanceTo(HexCell other)
    {
        var q = Q - other.Q;
        var r = R - other.R;

        return Math.Max(Math.Max(Math.Abs(q), Math.Abs(r)), Math.Abs(q + r));
    }
}
