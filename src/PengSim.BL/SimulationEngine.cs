using System.Collections.ObjectModel;
using PengSim.BL.Contracts;
using PengSim.BL.Model;

namespace PengSim.BL;

/// <inheritdoc />
public sealed class SimulationEngine : ISimulationEngine
{
    private ISimulation? _simulation;

    /// <inheritdoc />
    public IReadOnlyList<IEnvironment> Iterations => _simulation?.Frames ?? Array.Empty<IEnvironment>();

    /// <inheritdoc />
    public IReadOnlyDictionary<ushort, IPenguin> Penguins =>
        _simulation?.Penguins ?? ReadOnlyDictionary<ushort, IPenguin>.Empty;

    private ISimulation ActiveSimulation =>
        _simulation ?? throw new InvalidOperationException("Create a simulation before placing penguins or stepping.");

    /// <inheritdoc />
    public void CreateSimulation(int width, int height)
    {
        _simulation = new Simulation(width, height);
    }

    /// <inheritdoc />
    public ushort AddPenguin(ushort column, ushort row)
    {
        return ActiveSimulation.AddPenguin(column, row);
    }

    /// <inheritdoc />
    public void Step()
    {
        ActiveSimulation.Step();
    }
}
