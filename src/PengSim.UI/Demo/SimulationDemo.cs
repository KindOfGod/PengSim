using PengSim.BL.Contracts;
using PengSim.UI.Map;
using PengSim.UI.Rendering;

namespace PengSim.UI.Demo;

internal sealed class SimulationDemo(ISimulationEngine engine, EnvironmentDrawer drawer)
{
    public void Run()
    {
        var footprint = new HexFootprint(Radius: 80);
        var simulation = engine.CreateSimulation(footprint.Size, footprint.Size);
        var random = new Random(1024);
        var positions = new HashSet<(int Column, int Row)>();
        (int Column, int Row)[] clusters = [(45, 55), (115, 55), (80, 80), (45, 105), (115, 105)];

        foreach (var (column, row) in clusters)
        {
            int targetCount = positions.Count + 1000;
            while (positions.Count < targetCount)
            {
                int dx = random.Next(-24, 25);
                int dy = random.Next(-24, 25);
                int x = column + dx;
                int y = row + dy;
                if (dx * dx + dy * dy <= 24 * 24 && footprint.Contains(x, y) && positions.Add((x, y)))
                {
                    simulation.AddPenguin(x, y);
                }
            }
        }

        drawer.Show(simulation.Environment, footprint, simulationName: "Penguin colonies demo");
    }
}
