using PengSim.BL.Contracts;
using PengSim.BL.Model;

namespace PengSim.BL;

/// <summary>Creates and tracks the active simulation.</summary>
public sealed class SimulationEngine : ISimulationEngine
{
    /// <inheritdoc />
    public Simulation? CurrentSimulation { get; private set; }

    /// <inheritdoc />
    public Simulation CreateSimulation(int width, int height)
    {
        var simulation = new Simulation(width, height);
        CurrentSimulation = simulation;

        return simulation;
    }
}
