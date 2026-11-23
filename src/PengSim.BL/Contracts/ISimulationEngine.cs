namespace PengSim.BL.Contracts;

/// <summary>Creates, populates, and advances the active simulation and exposes its recorded iterations.</summary>
public interface ISimulationEngine
{
    /// <summary>Maximum number of penguins in one run.</summary>
    const ushort MaxPenguins = 65_000;

    /// <summary>Maximum width or height of a board in cells.</summary>
    const ushort MaxDimension = 65_000;

    /// <summary>Gets the active run's read-only position snapshots by iteration number, starting at zero. Empty before creation.</summary>
    IReadOnlyList<IEnvironment> Iterations { get; }

    /// <summary>Gets the active run's penguins by occupancy ID. Empty before creation.</summary>
    IReadOnlyDictionary<ushort, IPenguin> Penguins { get; }

    /// <summary>Starts a new, empty simulation with the given positive dimensions and resets the iteration history.</summary>
    void CreateSimulation(ushort width, ushort height);

    /// <summary>Places a penguin in an empty cell before the first step and returns its simulation-local ID.</summary>
    ushort AddPenguin(ushort column, ushort row);

    /// <summary>Advances the active simulation by one step and appends its completed positions to the history.</summary>
    void Step();
}
