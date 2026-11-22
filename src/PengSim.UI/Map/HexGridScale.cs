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

        // Cube coordinates obey q + r + s = 0. Independently rounding all
        // three axes could break that invariant, so correct the coordinate
        // with the largest rounding error after taking rounded values.
        var rq = Math.Round(q, MidpointRounding.AwayFromZero);
        var rr = Math.Round(r, MidpointRounding.AwayFromZero);
        var rs = Math.Round(s, MidpointRounding.AwayFromZero);

        var dq = Math.Abs(rq - q);
        var dr = Math.Abs(rr - r);
        var ds = Math.Abs(rs - s);

        // Deterministic tie-breaking ensures every base hex maps to exactly
        // one bucket, even on boundaries between two coarse hex centers.
        if (dq > dr && dq > ds)
        {
            rq = -rr - rs;
        }
        else if (dr > ds)
        {
            rr = -rq - rs;
        }

        return new HexCell((int)rq, (int)rr);
    }

    /// <summary>Returns the base-cell coordinate at a display bucket's center.</summary>
    public static HexCell ToBase(HexCell cell, int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, 30);

        // The inverse maps a coarse bucket center into base-level axial
        // coordinates. This is used for camera motion, not BL mutations.
        var scale = 1L << level;
        return new HexCell(
            checked((int)(cell.Q * scale)),
            checked((int)(cell.R * scale)));
    }
}
