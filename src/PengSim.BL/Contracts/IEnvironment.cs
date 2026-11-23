using PengSim.BL.Model;

namespace PengSim.BL.Contracts;

/// <summary>Read-only occupancy of a simulation grid in offset coordinates.</summary>
public interface IEnvironment
{
    /// <summary>Occupancy value for a cell without a penguin.</summary>
    const ushort EmptyCell = 0;

    /// <summary>Gets the number of columns.</summary>
    int Width { get; }

    /// <summary>Gets the number of rows.</summary>
    int Height { get; }

    /// <summary>Gets read-only positions in ID order. The position for ID n is at index n - 1.</summary>
    IReadOnlyList<PenguinPosition> Positions { get; }

    /// <summary>Searches this frame for the penguin ID at the given cell, returning zero for an empty cell.</summary>
    ushort this[int column, int row] { get; }
}
