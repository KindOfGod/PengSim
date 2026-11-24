namespace PengSim.BL.Contracts;

/// <summary>Creates and advances the active simulation and exposes its recorded iterations.</summary>
public interface ISimulationEngine
{
    /// <summary>Maximum number of penguins in one run.</summary>
    const ushort MaxPenguins = ushort.MaxValue;

    /// <summary>Largest Q or R a map cell can have, so A + C - 2 and B + C - 2 must not exceed it.</summary>
    const ushort MaxCoordinate = ushort.MaxValue;

    /// <summary>
    /// Gets the active run's read-only position snapshots by iteration number. Index zero holds the
    /// initial placement. Empty before a simulation is created.
    /// </summary>
    IReadOnlyList<ISimulationStep> Iterations { get; }

    /// <summary>Gets the active run's penguins by ID, starting at 1. Empty before a simulation is created.</summary>
    IReadOnlyDictionary<ushort, IPenguin> Penguins { get; }

    /// <summary>
    /// Validates the parameters, places the penguins, and makes the new simulation current.
    /// Throws an <see cref="ArgumentException"/> when the parameters are invalid.
    /// </summary>
    /// <returns>This engine, so calls can be chained.</returns>
    ISimulationEngine CreateSimulation(ISimulationParameters parameters);

    /// <summary>
    /// Decodes a code created by <see cref="ISimulationParametersCodec.Encode"/> and creates the
    /// simulation it describes, like <see cref="CreateSimulation(ISimulationParameters)"/>.
    /// Throws a <see cref="FormatException"/>, <see cref="OverflowException"/>, or
    /// <see cref="ArgumentException"/> when the code is malformed or describes invalid parameters.
    /// </summary>
    /// <returns>This engine, so calls can be chained.</returns>
    ISimulationEngine CreateSimulation(string code);

    /// <summary>
    /// Advances the active simulation by one step and appends its positions to <see cref="Iterations"/>.
    /// Throws an <see cref="InvalidOperationException"/> before a simulation is created.
    /// </summary>
    void Step();

    /// <summary>
    /// Calculates all remaining steps until <see cref="ISimulationParameters.SimulatedSteps"/> steps are recorded.
    /// Throws an <see cref="InvalidOperationException"/> before a simulation is created.
    /// </summary>
    void Exec();
}
