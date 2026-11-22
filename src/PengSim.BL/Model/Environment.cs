namespace PengSim.BL.Model;

/// <summary>A pointy-topped hex grid with odd rows offset right by half a cell.</summary>
public sealed class Environment
{
    public const int EmptyCell = 0;

    private readonly int[,] _cells;

    internal Environment(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _cells = new int[height, width];
    }

    public int Width => _cells.GetLength(1);
    public int Height => _cells.GetLength(0);

    public int this[int column, int row]
    {
        get => _cells[row, column];
        internal set => _cells[row, column] = value;
    }
}
