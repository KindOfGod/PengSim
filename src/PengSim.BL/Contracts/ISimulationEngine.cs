namespace PengSim.BL.Contracts;

/// <summary>Creates simulations and exposes the active simulation.</summary>
public interface ISimulationEngine
{
    /// <summary>Gets the current simulation, if one has been created.</summary>
    ISimulation? CurrentSimulation { get; }

    /// <summary>Creates a simulation of the given dimensions and makes it current.</summary>
    ISimulation CreateSimulation(int width, int height);
}
