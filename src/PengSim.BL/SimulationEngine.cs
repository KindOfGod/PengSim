using System.Collections.ObjectModel;
using PengSim.BL.Contracts;
using PengSim.BL.Model;
using PengSim.BL.Parameters;

namespace PengSim.BL;

/// <inheritdoc />
public sealed class SimulationEngine : ISimulationEngine
{
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
        if (_simulation is null) throw new InvalidOperationException("Simulation was not created yet.");
        
        _simulation.Step();
    }

    public void Exec()
    {
        if (_simulation is null) throw new InvalidOperationException("Simulation was not created yet.");

        _simulation?.Exec();
    }
}
