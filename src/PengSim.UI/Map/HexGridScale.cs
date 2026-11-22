namespace PengSim.UI.Map;

/// <summary>
/// Display-only hexagonal aggregation on a world-aligned axial lattice.
/// Multiplying both axial axes by a power of two preserves their orientation:
/// no rotation or skew is introduced when zooming.
/// </summary>
internal static class HexGridScale
{
    /// <summary>
    /// Rounds a base-level hex to its nearest hex center on a coarser,
    /// axis-aligned lattice. Deterministic cube-coordinate rounding assigns
    /// each base cell to exactly one display bucket.
    /// </summary>
    public static HexCell ToLevel(HexCell cell, int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, 30);

        var scale = (double)(1L << level);
        var q = cell.Q / scale;
        var r = cell.R / scale;
        var s = -q - r;

        var rq = Math.Round(q, MidpointRounding.AwayFromZero);
        var rr = Math.Round(r, MidpointRounding.AwayFromZero);
        var rs = Math.Round(s, MidpointRounding.AwayFromZero);

        var dq = Math.Abs(rq - q);
        var dr = Math.Abs(rr - r);
        var ds = Math.Abs(rs - s);

        if (dq > dr && dq > ds)
        {
            rq = -rr - rs;
        }
        else if (dr > ds)
        {
            rr = -rq - rs;
        }
        else
        {
            rs = -rq - rr;
        }

        return new HexCell((int)rq, (int)rr);
    }

    /// <summary>Returns the base-cell coordinate at a display bucket's center.</summary>
    public static HexCell ToBase(HexCell cell, int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, 30);

        var scale = 1L << level;
        return new HexCell(
            checked((int)(cell.Q * scale)),
            checked((int)(cell.R * scale)));
    }
}
