namespace PengSim.BL.Contracts;

/// <summary>Stores one run and applies the operations requested by the simulation engine.</summary>
internal interface ISimulation
{
    /// <summary>Gets the position snapshots in iteration order, including initial placement at index zero.</summary>
    IReadOnlyList<ISimulationStep> Frames { get; }

    /// <summary>Gets the validated parameters this run was created from.</summary>
    ISimulationParameters Parameters { get; }

    /// <summary>Gets the penguins in this run by ID, starting at 1.</summary>
    IReadOnlyDictionary<ushort, IPenguin> Penguins { get; }

    /// <summary>Moves each penguin once, in ID order, and records the resulting positions as a new frame.</summary>
    void Step();

    /// <summary>
    /// Calls <see cref="Step"/> until <see cref="ISimulationParameters.SimulatedSteps"/> steps are recorded.
    /// Steps that were already calculated are kept, so calling it again does nothing.
    /// </summary>
    void Exec();
}
