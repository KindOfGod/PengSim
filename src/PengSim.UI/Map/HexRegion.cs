namespace PengSim.UI.Map;

/// <summary>A regular hexagonal region on one level of the lattice.</summary>
internal readonly record struct HexRegion(HexCell Center, int Radius)
{
    /// <summary>Checks whether a cell is on or within the perimeter.</summary>
    public bool Contains(HexCell cell) => cell.DistanceTo(Center) <= Radius;

    /// <summary>Enumerates the cells in this region in row order.</summary>
    public IEnumerable<HexCell> Cells()
    {
        for (var r = -Radius; r <= Radius; r++)
        {
            var minQ = Math.Max(-Radius, -r - Radius);
            var maxQ = Math.Min(Radius, -r + Radius);

            for (var q = minQ; q <= maxQ; q++)
            {
                yield return new HexCell(Center.Q + q, Center.R + r);
            }
        }
    }
}
