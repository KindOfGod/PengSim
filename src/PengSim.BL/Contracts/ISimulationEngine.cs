using PengSim.BL.Model;

namespace PengSim.BL.Contracts;

public interface ISimulationEngine
{
    Simulation? CurrentSimulation { get; }

    Simulation CreateSimulation(int width, int height);
}
