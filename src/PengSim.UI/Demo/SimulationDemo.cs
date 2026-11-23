using System.Diagnostics;
using PengSim.BL.Contracts;
using PengSim.UI.Map;
using PengSim.UI.Rendering;

namespace PengSim.UI.Demo;

internal sealed class SimulationDemo(
    ISimulationEngine engine, IEnvironmentDrawer drawer, Func<IDemoScreen> createScreen) : ISimulationDemo
{
    private const int IterationCount = 1000;
    private const int Population = 5000;
    private static readonly HexFootprint Board = new(Radius: 150);
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
        var cellCount = PlacePenguins();

        var iterationTime = Stopwatch.StartNew();
        var nextUpdate = TimeSpan.Zero;
        for (var step = 1; step <= IterationCount; step++)
        {
            engine.Step();
            var elapsed = iterationTime.Elapsed;
            if (screen is not null && (elapsed >= nextUpdate || step == IterationCount))
            {
                screen.ShowProgress(GetProgress(cellCount, totalTime.Elapsed, elapsed));
                nextUpdate = elapsed + ProgressInterval;
            }
        }

        iterationTime.Stop();
        totalTime.Stop();
        return GetProgress(cellCount, totalTime.Elapsed, iterationTime.Elapsed);
    }

    private int PlacePenguins()
    {
        var cells = Board.Region.Cells().ToArray();
        Random.Shared.Shuffle(cells);
        foreach (var cell in cells.Take(Population))
        {
            var (column, row) = Board.ToOffset(cell);
            engine.AddPenguin(checked((ushort)column), checked((ushort)row));
        }

        return cells.Length;
    }

    private DemoProgress GetProgress(int cellCount, TimeSpan totalTime, TimeSpan iterationTime)
    {
        return new DemoProgress(engine.Iterations.Count - 1, IterationCount, totalTime,
            iterationTime, engine.Penguins.Count, Board.Radius, cellCount);
    }
}
