using PengSim.BL.Model;

namespace PengSim.BL.Contracts;

/// <summary>Read-only penguin positions of one iteration on the hexagonal map.</summary>
public interface IEnvironment
{
    /// <summary>Gets the map these positions lie on.</summary>
    IHexMap Map { get; }

    /// <summary>Gets read-only positions in ID order. The position for ID n is at index n - 1.</summary>
    IReadOnlyList<PenguinPosition> Positions { get; }

    /// <summary>Searches this frame for the penguin ID at the given cell, returning null for an empty cell.</summary>
    ushort? this[PenguinPosition position] { get; }
}
