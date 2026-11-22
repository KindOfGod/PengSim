using PengSim.BL.Contracts;

namespace PengSim.UI;

internal sealed class SimulationDemo(ISimulationEngine engine, EnvironmentDrawer drawer)
{
    public void Run()
    {
        var simulation = engine.CreateSimulation(width: 5, height: 5);
        simulation.AddPenguin(column: 2, row: 0);
        simulation.AddPenguin(column: 1, row: 1);
        simulation.AddPenguin(column: 2, row: 2);
        simulation.AddPenguin(column: 2, row: 3);

        drawer.Draw(simulation.Environment);
    }
}
