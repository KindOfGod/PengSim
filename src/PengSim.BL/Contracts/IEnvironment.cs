using PengSim.BL.Model;

namespace PengSim.BL.Contracts;

/// <summary>Read-only occupancy of a simulation grid in offset coordinates.</summary>
public interface IEnvironment
{
    /// <summary>Gets the number of columns.</summary>
    uint Width { get; }

    /// <summary>Gets the number of rows.</summary>
    uint Height { get; }

    /// <summary>Gets read-only positions in ID order. The position for ID n is at index n - 1.</summary>
    IReadOnlyList<PenguinPosition> Positions { get; }

    /// <summary>Searches this frame for the penguin ID at the given cell, returning zero for an empty cell.</summary>
    ushort? this[ushort column, ushort row] { get; }
}
