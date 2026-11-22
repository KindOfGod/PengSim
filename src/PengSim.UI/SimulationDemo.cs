using PengSim.BL.Contracts;

namespace PengSim.UI;

internal sealed class SimulationDemo(ISimulationEngine engine, EnvironmentDrawer drawer)
{
    public void Run()
    {
        var simulation = engine.CreateSimulation(width: 240, height: 120);
        var random = new Random(1024);
        var positions = new HashSet<(int Column, int Row)>();
        (int Column, int Row)[] clusters = [(35, 32), (85, 83), (120, 60), (162, 30), (204, 85)];

        foreach (var (column, row) in clusters)
        {
            int targetCount = positions.Count + 1000;
            while (positions.Count < targetCount)
            {
                int dx = random.Next(-24, 25);
                int dy = random.Next(-24, 25);
                if (dx * dx + dy * dy <= 24 * 24 && positions.Add((column + dx, row + dy)))
                {
                    simulation.AddPenguin(column + dx, row + dy);
                }
            }
        }

        drawer.Show(simulation.Environment);
    }
}
