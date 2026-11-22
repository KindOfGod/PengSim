using PengSim.BL.Model;

namespace PengSim.BL.Contracts;

/// <summary>Creates simulations and exposes the active simulation.</summary>
public interface ISimulationEngine
{
    /// <summary>Gets the current simulation, if one has been created.</summary>
    Simulation? CurrentSimulation { get; }

    /// <summary>Creates a simulation of the given dimensions and makes it current.</summary>
    Simulation CreateSimulation(int width, int height);
}
