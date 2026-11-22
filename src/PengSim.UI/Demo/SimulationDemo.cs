using PengSim.BL.Contracts;
using PengSim.BL.Model;
using PengSim.UI.Map;
using PengSim.UI.Rendering;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Demo;

/// <summary>Builds the repeatable snapshot used by the console demo.</summary>
internal sealed class SimulationDemo(ISimulationEngine engine, EnvironmentDrawer drawer)
{
    /// <summary>Creates the demo simulation and opens its console view.</summary>
    public void Run()
    {
        var footprint = new HexFootprint(Radius: 80);
        var simulation = engine.CreateSimulation(footprint.Size, footprint.Size);

        PopulateClusters(simulation, footprint);

        drawer.Show(simulation.Environment, footprint, simulationName: "Penguin colonies demo");
    }

    /// <summary>Places 5,000 penguins in five clusters using the same seed on every run.</summary>
    private static void PopulateClusters(Simulation simulation, HexFootprint footprint)
    {
        const int seed = 1024;
        const int penguinsPerCluster = 1000;
        const int clusterRadius = 24;

        var random = new Random(seed);
        (int Column, int Row)[] clusters = [(45, 55), (115, 55), (80, 80), (45, 105), (115, 105)];

        foreach (var (column, row) in clusters)
        {
            var targetCount = simulation.Penguins.Count + penguinsPerCluster;

            // A sample only counts when it is inside the cluster and occupies a new playable cell.
            while (simulation.Penguins.Count < targetCount)
            {
                var dx = random.Next(-clusterRadius, clusterRadius + 1);
                var dy = random.Next(-clusterRadius, clusterRadius + 1);
                var x = column + dx;
                var y = row + dy;

                if (dx * dx + dy * dy <= clusterRadius * clusterRadius
                    && footprint.Contains(x, y)
                    && simulation.Environment[x, y] == HexEnvironment.EmptyCell)
                {
                    simulation.AddPenguin(x, y);
                }
            }
        }
    }
}
