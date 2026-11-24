namespace PengSim.BL.Contracts;

/// <summary>Creates, populates, and advances the active simulation and exposes its recorded iterations.</summary>
public interface ISimulationEngine
{
    /// <summary>Maximum number of penguins in one run.</summary>
    const ushort MaxPenguins = ushort.MaxValue;

    /// <summary>Maximum width or height of a board in cells.</summary>
    const ushort MaxDimension = ushort.MaxValue;

    /// <summary>Gets the active run's read-only position snapshots by iteration number, starting at zero. Empty before creation.</summary>
    IReadOnlyList<IEnvironment> Iterations { get; }

    /// <summary>Gets the active run's penguins by occupancy ID. Empty before creation.</summary>
    IReadOnlyDictionary<ushort, IPenguin> Penguins { get; }

    /// <summary>Creates a simulation from the given parameters and makes it current.</summary>
    ISimulationEngine CreateSimulation(ISimulationParameters parameters);

    /// <summary>
    /// Loads a config from a hash and creates a simulation from the
    /// configs parameters.
    /// </summary>
    /// <param name="code">Encoded parameters</param>
    /// <returns>The created simulation object</returns>
    ISimulationEngine CreateSimulation(string code);
    
    /// <summary>Advances the active simulation by one step and appends its completed positions to the history.</summary>
    void Step();

    /// <summary>
    /// Runs the whole simulation directly
    /// </summary>
    void Exec();
}
