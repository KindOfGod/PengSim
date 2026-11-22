namespace PengSim.BL.Model;

/// <summary>A pointy-topped hex grid with odd rows offset right by half a cell.</summary>
public sealed class Environment
{
    /// <summary>The value stored for cells without a penguin.</summary>
    public const int EmptyCell = 0;

    private readonly int[,] _cells;

    /// <summary>Allocates an empty grid with the given dimensions.</summary>
    internal Environment(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _cells = new int[height, width];
    }

    /// <summary>Gets the number of columns.</summary>
    public int Width => _cells.GetLength(1);

    /// <summary>Gets the number of rows.</summary>
    public int Height => _cells.GetLength(0);

    /// <summary>Gets the occupancy ID at a column and row, or zero for an empty cell.</summary>
    public int this[int column, int row]
    {
        get => _cells[row, column];
        internal set => _cells[row, column] = value;
    }
}
