using System.Diagnostics;
using PengSim.BL.Contracts;
using PengSim.UI.Map;
using PengSim.UI.Rendering;

namespace PengSim.UI.Demo;

internal sealed class SimulationDemo(ISimulationEngine engine, EnvironmentDrawer drawer)
{
    private const int Steps = 1000;
    private const int Population = 5000;

    public void Run()
    {
        var interactive = !Console.IsInputRedirected && !Console.IsOutputRedirected;
        if (interactive)
        {
            Console.WriteLine("PengSim\n\n1  Create simulation (coming soon)\n2  Run demo\nQ  Quit");
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Q) return;
                if (key.KeyChar == '2') break;
            }
        }

        var footprint = new HexFootprint(Radius: 100);
        var simulation = engine.CreateSimulation(footprint.Size, footprint.Size);
        var cells = footprint.Region.Cells().ToArray();
        Random.Shared.Shuffle(cells);
        foreach (var cell in cells.Take(Population))
        {
            var (column, row) = footprint.ToOffset(cell);
            simulation.AddPenguin(column, row);
        }

        var clock = Stopwatch.StartNew();
        var nextUpdate = TimeSpan.Zero;
        if (interactive) Console.WriteLine("\nCalculating demo");
        for (var step = 1; step <= Steps; step++)
        {
            simulation.Step();
            var elapsed = clock.Elapsed;
            if (interactive && (elapsed >= nextUpdate || step == Steps))
            {
                var filled = 20 * step / Steps;
                Console.Write($"\r[{new string('#', filled)}{new string('-', 20 - filled)}] "
                    + $"{step,4}/{Steps}  {step / elapsed.TotalSeconds,7:F1} iterations/s");
                nextUpdate = elapsed + TimeSpan.FromMilliseconds(100);
            }
        }

        if (interactive) Console.WriteLine();
        drawer.Show(simulation.Frames, footprint, "Penguin demo");
    }
}
