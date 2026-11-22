namespace PengSim.UI;

internal readonly record struct HexFootprint(int Radius)
{
    public int Size => Radius * 2 + 1;

    public (int Q, int R) ToAxial(int column, int row) =>
        (column - Radius - (row >> 1) + (Radius >> 1), row - Radius);

    public bool Contains(int column, int row)
    {
        var (q, r) = ToAxial(column, row);
        return Distance(q, r) <= Radius;
    }

    public static int Distance(int q, int r) => Math.Max(Math.Max(Math.Abs(q), Math.Abs(r)), Math.Abs(q + r));
}
