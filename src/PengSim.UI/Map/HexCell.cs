namespace PengSim.UI.Map;

/// <summary>An axial coordinate in a base or aggregate hex lattice.</summary>
internal readonly record struct HexCell(int Q, int R)
{
    // Removing one of these seven offsets makes the inverse parent transform integral.
    private static readonly HexCell[] ChildOffsets =
    [
        new(0, 0), new(0, 1), new(1, -1), new(1, 0),
        new(-1, 0), new(-1, 1), new(0, -1),
    ];

    /// <summary>Returns the number of hex steps between two cells on the same level.</summary>
    public int DistanceTo(HexCell other)
    {
        var q = Q - other.Q;
        var r = R - other.R;

        return Math.Max(Math.Max(Math.Abs(q), Math.Abs(r)), Math.Abs(q + r));
    }

    /// <summary>Projects the cell up the given number of levels.</summary>
    public HexCell Ancestor(int level)
    {
        var cell = this;

        for (var i = 0; i < level; i++)
        {
            cell = cell.Parent();
        }

        return cell;
    }

    /// <summary>Returns the geometric center of a parent in the finer lattice.</summary>
    public HexCell DescendToCenter(int level)
    {
        var cell = this;

        for (var i = 0; i < level; i++)
        {
            cell = new HexCell(2 * cell.Q - cell.R, cell.Q + 3 * cell.R);
        }

        return cell;
    }

    /// <summary>Finds the unique parent whose seven-child group contains this cell.</summary>
    public HexCell Parent()
    {
        // Coarse basis vectors (2, 1) and (-1, 3) define an index-seven hex lattice.
        var residue = (3L * Q + R) % 7;
        var offset = ChildOffsets[(int)((residue + 7) % 7)];
        var q = (long)Q - offset.Q;
        var r = (long)R - offset.R;

        return new HexCell((int)((3 * q + r) / 7), (int)((2 * r - q) / 7));
    }
}
