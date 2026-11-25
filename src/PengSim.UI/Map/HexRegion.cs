namespace PengSim.UI.Map;

/// <summary>
/// A hexagon on one level of the lattice whose opposite sides have equal length.
/// </summary>
/// <remarks>
/// Such a hexagon is exactly the set of cells whose Q, R, and S = -Q - R each lie within
/// a range. A regular hexagon of radius n has all three ranges at [-n, n].
/// </remarks>
internal readonly record struct HexRegion(int MinQ, int MaxQ, int MinR, int MaxR, int MinS, int MaxS)
{
    /// <summary>Gets the cell at the midpoint of the Q and R ranges, rounded toward zero.</summary>
    public HexCell Center => new((MinQ + MaxQ) / 2, (MinR + MaxR) / 2);

    /// <summary>Gets the number of rows the region covers.</summary>
    public int RowCount => MaxR - MinR + 1;

    /// <summary>Checks whether a cell is on or within the perimeter.</summary>
    public bool Contains(HexCell cell)
    {
        var s = -cell.Q - cell.R;
        return cell.Q >= MinQ && cell.Q <= MaxQ
            && cell.R >= MinR && cell.R <= MaxR
            && s >= MinS && s <= MaxS;
    }

    /// <summary>Returns the region grown by <paramref name="cells"/> on every side.</summary>
    public HexRegion Expand(int cells)
    {
        return new HexRegion(MinQ - cells, MaxQ + cells, MinR - cells, MaxR + cells, MinS - cells, MaxS + cells);
    }

    /// <summary>Returns the first and last Q of the region's cells in row <paramref name="r"/>.</summary>
    public (int FirstQ, int LastQ) GetRow(int r)
    {
        return (Math.Max(MinQ, -r - MaxS), Math.Min(MaxQ, -r - MinS));
    }

    /// <summary>
    /// Returns the leftmost and rightmost screen column of the region's cells, where a cell
    /// is drawn at column 2 * Q + R.
    /// </summary>
    public (int Left, int Right) GetScreenColumns()
    {
        var left = int.MaxValue;
        var right = int.MinValue;
        for (var r = MinR; r <= MaxR; r++)
        {
            var (firstQ, lastQ) = GetRow(r);
            left = Math.Min(left, 2 * firstQ + r);
            right = Math.Max(right, 2 * lastQ + r);
        }

        return (left, right);
    }
}
