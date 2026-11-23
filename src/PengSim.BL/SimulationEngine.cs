using PengSim.BL.Contracts;
using PengSim.BL.Model;

namespace PengSim.BL;

/// <summary>Creates and tracks the active simulation.</summary>
public sealed class SimulationEngine : ISimulationEngine
{
    /// <inheritdoc />
    public ISimulation? CurrentSimulation { get; private set; }

    /// <inheritdoc />
    public ISimulation CreateSimulation(int width, int height)
    {
        ISimulation simulation = new Simulation(width, height);
        CurrentSimulation = simulation;

        return simulation;
    }
}
