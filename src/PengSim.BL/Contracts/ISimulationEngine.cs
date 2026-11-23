namespace PengSim.BL.Contracts;

/// <summary>Creates, populates, and advances the active simulation and exposes its recorded iterations.</summary>
public interface ISimulationEngine
{
    /// <summary>Gets the active run's read-only grids by iteration number, starting at zero. Empty before creation.</summary>
    IReadOnlyList<IEnvironment> Iterations { get; }

    /// <summary>Gets the active run's penguins by occupancy ID. Empty before creation.</summary>
    IReadOnlyDictionary<int, IPenguin> Penguins { get; }

    /// <summary>Starts a new, empty simulation with the given positive dimensions and resets the iteration history.</summary>
    void CreateSimulation(int width, int height);

    /// <summary>Places a penguin in an empty cell before the first step and returns its simulation-local ID.</summary>
    int AddPenguin(int column, int row);

    /// <summary>Advances the active simulation by one step and appends its completed grid to the history.</summary>
    void Step();
}
