using PengSim.BL.Model;

namespace PengSim.BL.Contracts;

/// <summary>Read-only penguin positions of one iteration on the hexagonal map.</summary>
public interface ISimulationStep
{
    /// <summary>Gets the map these positions lie on.</summary>
    IHexMap Map { get; }

    /// <summary>Gets read-only positions in ID order. The position for ID n is at index n - 1.</summary>
    IReadOnlyList<PenguinPosition> Positions { get; }

    /// <summary>
    /// Gets the ID of the penguin on the given cell, or null for an empty cell.
    /// The lookup searches <see cref="Positions"/>, so it takes time proportional to the population.
    /// </summary>
    ushort? this[PenguinPosition position] { get; }
}
