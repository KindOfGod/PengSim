namespace PengSim.UI;

internal readonly record struct HexCell(int Q, int R)
{
    // Removing one of these seven offsets makes the inverse parent transform integral.
    private static readonly HexCell[] ChildOffsets =
    [
        new(0, 0), new(0, 1), new(1, -1), new(1, 0),
        new(-1, 0), new(-1, 1), new(0, -1),
    ];

    public int DistanceTo(HexCell other)
    {
        int q = Q - other.Q;
        int r = R - other.R;
        return Math.Max(Math.Max(Math.Abs(q), Math.Abs(r)), Math.Abs(q + r));
    }

    public HexCell Parent()
    {
        // Coarse basis vectors (2, 1) and (-1, 3) define an index-seven hex lattice.
        long residue = (3L * Q + R) % 7;
        var offset = ChildOffsets[(int)((residue + 7) % 7)];
        long q = (long)Q - offset.Q;
        long r = (long)R - offset.R;
        return new HexCell((int)((3 * q + r) / 7), (int)((2 * r - q) / 7));
    }
}

internal readonly record struct HexRegion(HexCell Center, int Radius)
{
    public bool Contains(HexCell cell) => cell.DistanceTo(Center) <= Radius;

    public IEnumerable<HexCell> Cells()
    {
        for (int r = -Radius; r <= Radius; r++)
        {
            int minQ = Math.Max(-Radius, -r - Radius);
            int maxQ = Math.Min(Radius, -r + Radius);
            for (int q = minQ; q <= maxQ; q++)
            {
                yield return new HexCell(Center.Q + q, Center.R + r);
            }
        }
    }
}

internal readonly record struct HexFootprint(int Radius)
{
    public int Size => Radius * 2 + 1;
    public HexRegion Region => new(default, Radius);

    public HexCell ToAxial(int column, int row) =>
        new(column - Radius - (row >> 1) + (Radius >> 1), row - Radius);

    public (int Column, int Row) ToOffset(HexCell cell)
    {
        int row = cell.R + Radius;
        return (cell.Q + Radius + (row >> 1) - (Radius >> 1), row);
    }

    public bool Contains(int column, int row) => Region.Contains(ToAxial(column, row));
}
