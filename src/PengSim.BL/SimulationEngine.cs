using System.Collections.ObjectModel;
using PengSim.BL.Contracts;
using PengSim.BL.Model;

namespace PengSim.BL;

/// <inheritdoc />
public sealed class SimulationEngine : ISimulationEngine
{
    /// <summary>The active run, or null before the first call to <see cref="CreateSimulation(ISimulationParameters)"/>.</summary>
    private ISimulation? _simulation;

    /// <inheritdoc />
    public IReadOnlyList<ISimulationStep> Iterations => _simulation?.Frames ?? Array.Empty<ISimulationStep>();

    /// <inheritdoc />
    public IReadOnlyDictionary<ushort, IPenguin> Penguins =>
        _simulation?.Penguins ?? ReadOnlyDictionary<ushort, IPenguin>.Empty;

    /// <inheritdoc />
    public ISimulationEngine CreateSimulation(ISimulationParameters parameters)
    {
        _simulation = new Simulation(parameters);
        return this;
    }

    /// <inheritdoc />
    public ISimulationEngine CreateSimulation(string code)
    {
        _simulation = new Simulation(code);
        return this;
    }

    /// <inheritdoc />
    public void Step()
    {
        GetActiveSimulation().Step();
    }

    /// <inheritdoc />
    public void Exec()
    {
        GetActiveSimulation().Exec();
    }

    /// <summary>Returns the active run, or throws when no simulation was created yet.</summary>
    private ISimulation GetActiveSimulation()
    {
        if (_simulation is null)
        {
            throw new InvalidOperationException("Simulation was not created yet.");
        }

        return _simulation;
    }
}
