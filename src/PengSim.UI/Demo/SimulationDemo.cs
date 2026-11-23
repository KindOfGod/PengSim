using System.Diagnostics;
using PengSim.BL.Contracts;
using PengSim.BL.Model;
using PengSim.UI.Map;
using PengSim.UI.Rendering;

namespace PengSim.UI.Demo;

internal sealed class SimulationDemo(
    ISimulationEngine engine, IEnvironmentDrawer drawer, Func<IDemoScreen> createScreen) : ISimulationDemo
{
    private const int IterationCount = 100_000;
    private static readonly HexFootprint Board = new(ISimulationEngine.MaxDimension);
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(100);

    public void Run()
    {
        var interactive = !Console.IsInputRedirected && !Console.IsOutputRedirected;
        using var screen = interactive ? createScreen() : null;
        if (screen is not null && !screen.WaitForDemoStart())
        {
            return;
        }

        var result = GenerateDemo(screen);
        if (screen is not null && !screen.WaitForAnalysis(result))
        {
            return;
        }

        drawer.Show(engine.Iterations, Board, "Penguin demo");
    }

    private DemoProgress GenerateDemo(IDemoScreen? screen)
    {
        var totalTime = Stopwatch.StartNew();
        engine.CreateSimulation(Board.Size, Board.Size);
        PlacePenguins();

        var iterationTime = Stopwatch.StartNew();
        var nextUpdate = TimeSpan.Zero;
        for (var step = 1; step <= IterationCount; step++)
        {
            engine.Step();
            var elapsed = iterationTime.Elapsed;
            if (screen is not null && (elapsed >= nextUpdate || step == IterationCount))
            {
                screen.ShowProgress(GetProgress(totalTime.Elapsed, elapsed));
                nextUpdate = elapsed + ProgressInterval;
            }
        }

        iterationTime.Stop();
        totalTime.Stop();
        return GetProgress(totalTime.Elapsed, iterationTime.Elapsed);
    }

    private void PlacePenguins()
    {
        var placed = new HashSet<PenguinPosition>(ISimulationEngine.MaxPenguins);
        while (placed.Count < ISimulationEngine.MaxPenguins)
        {
            var x = (ushort)Random.Shared.Next(Board.Size);
            var y = (ushort)Random.Shared.Next(Board.Size);
            if (Board.Contains(x, y) && placed.Add(new PenguinPosition(x, y)))
            {
                engine.AddPenguin(x, y);
            }
        }
    }

    private DemoProgress GetProgress(TimeSpan totalTime, TimeSpan iterationTime)
    {
        return new DemoProgress(engine.Iterations.Count - 1, IterationCount, totalTime,
            iterationTime, engine.Penguins.Count, Board.Size);
    }
}
