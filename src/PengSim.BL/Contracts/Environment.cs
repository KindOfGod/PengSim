namespace PengSim.BL.Contracts;

/// <summary>A read-only view of occupancy in one simulation frame.</summary>
public sealed class Environment : IEnvironment
{
    private readonly int[,] _cells;

    public int Width => _cells.GetLength(1);
    public int Height => _cells.GetLength(0);
    public int this[int column, int row] => _cells[row, column];

    internal Environment(int[,] cells)
    {
        _cells = cells;
    }
}
