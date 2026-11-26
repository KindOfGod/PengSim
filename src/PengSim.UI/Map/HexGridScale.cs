namespace PengSim.UI.Map;

/// <summary>
/// Display-only hexagonal aggregation on a world-aligned axial lattice.
/// Multiplying both axial axes by a power of two preserves their orientation:
/// no rotation or skew is introduced when zooming.
/// </summary>
internal sealed class HexGridScale : IHexGridScale
{
    /// <inheritdoc />
    public HexCell ToLevel(HexCell cell, int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, 30);

        if (level == 0)
        {
            return cell;
        }

        var scale = 1L << level;
        // The nearest center's Q and R lie at the floor or ceiling of each scaled
        // coordinate. Right shift gives the floor, including for negative coordinates.
        var minQ = cell.Q >> level;
        var minR = cell.R >> level;
        var nearestDistance = long.MaxValue;
        var nearestRadius = long.MaxValue;
        var nearestTurn = long.MinValue;
        var nearest = default(HexCell);

        for (var q = minQ; q <= minQ + 1; q++)
        {
            for (var r = minR; r <= minR + 1; r++)
            {
                var deltaQ = cell.Q - q * scale;
                var deltaR = cell.R - r * scale;
                var distance = deltaQ * deltaQ + deltaQ * deltaR + deltaR * deltaR;
                if (distance > nearestDistance)
                {
                    continue;
                }

                var radius = Math.Max(Math.Max(Math.Abs(q), Math.Abs(r)), Math.Abs((long)q + r));
                // Equal-distance centers on the same ring are ordered by their signed
                // turn around the origin. The cross product keeps that order under rotation.
                var turn = (long)q * cell.R - (long)r * cell.Q;
                if (distance == nearestDistance &&
                    (radius > nearestRadius || (radius == nearestRadius && turn <= nearestTurn)))
                {
                    continue;
                }

                nearest = new HexCell(q, r);
                nearestDistance = distance;
                nearestRadius = radius;
                nearestTurn = turn;
            }
        }

        return nearest;
    }

    /// <summary>Returns the base-cell coordinate at a display bucket's center.</summary>
    public HexCell ToBase(HexCell cell, int level)
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
