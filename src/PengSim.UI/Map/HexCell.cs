namespace PengSim.UI.Map;

/// <summary>
/// A centered axial drawing coordinate in a base or aggregate hex lattice.
/// (0, 0) is the map center and R grows downward on screen.
/// </summary>
public readonly record struct HexCell(int Q, int R)
{
    /// <summary>Returns the number of hex steps between two cells.</summary>
    public int DistanceTo(HexCell other)
    {
        var q = Q - other.Q;
        var r = R - other.R;

        return Math.Max(Math.Max(Math.Abs(q), Math.Abs(r)), Math.Abs(q + r));
    }
}
