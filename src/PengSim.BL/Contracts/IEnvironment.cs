namespace PengSim.BL.Contracts;

/// <summary>Read-only occupancy of a simulation grid in offset coordinates.</summary>
public interface IEnvironment
{
    /// <summary>Occupancy value for a cell without a penguin.</summary>
    const int EmptyCell = 0;

    /// <summary>Gets the number of columns.</summary>
    int Width { get; }

    /// <summary>Gets the number of rows.</summary>
    int Height { get; }

    /// <summary>Gets the penguin ID at the zero-based column and row, or zero for an empty cell.</summary>
    int this[int column, int row] { get; }
}
