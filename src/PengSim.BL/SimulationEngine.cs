using PengSim.BL.Contracts;
using PengSim.BL.Model;

namespace PengSim.BL;

public sealed class SimulationEngine : ISimulationEngine
{
    public Simulation? CurrentSimulation { get; private set; }

    public Simulation CreateSimulation(int width, int height)
    {
        var simulation = new Simulation(width, height);
        CurrentSimulation = simulation;
        return simulation;
    }
}
