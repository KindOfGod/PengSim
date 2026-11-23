using System.Diagnostics;
using PengSim.BL.Contracts;
using PengSim.UI.Map;
using PengSim.UI.Rendering;

namespace PengSim.UI.Demo;

internal sealed class SimulationDemo(
    ISimulationEngine engine, IEnvironmentDrawer drawer, Func<IDemoScreen> createScreen) : ISimulationDemo
{
    private const int Steps = 1000;
    private const int Population = 5000;

    public void Run()
    {
        var interactive = !Console.IsInputRedirected && !Console.IsOutputRedirected;
        using var screen = interactive ? createScreen() : null;
        const uint accent = IHeatmapPalette.Accent;
        const uint normal = IHeatmapPalette.DefaultColor;
        if (screen is not null && !screen.WaitFor('2', () => screen.Draw("Main menu",
            ("Create simulation (coming soon)", 0x949494),
            ("[2] Run demo", accent),
            ("", normal),
            ("[Q] Quit", normal))))
        {
            return;
        }

        var total = Stopwatch.StartNew();
        var footprint = new HexFootprint(Radius: 150);
        engine.CreateSimulation(footprint.Size, footprint.Size);
        var cells = footprint.Region.Cells().ToArray();
        Random.Shared.Shuffle(cells);
        foreach (var cell in cells.Take(Population))
        {
            var (column, row) = footprint.ToOffset(cell);
            engine.AddPenguin(column, row);
        }

        var clock = Stopwatch.StartNew();
        var nextUpdate = TimeSpan.Zero;

        for (var step = 1; step <= Steps; step++)
        {
            engine.Step();
            var elapsed = clock.Elapsed;
            if (interactive && (elapsed >= nextUpdate || step == Steps))
            {
                DrawProgress(step, elapsed, completed: false);
                nextUpdate = elapsed + TimeSpan.FromMilliseconds(100);
            }
        }

        clock.Stop();
        total.Stop();
        if (screen is not null && !screen.WaitFor('A',
            () => DrawProgress(Steps, clock.Elapsed, completed: true)))
        {
            return;
        }

        drawer.Show(engine.Iterations, footprint, "Penguin demo");

        void DrawProgress(int step, TimeSpan elapsed, bool completed)
        {
            const int barWidth = 22;
            var filled = barWidth * step / Steps;
            var bar = new string('█', filled) + new string('░', barWidth - filled);
            screen!.Draw(completed ? "Demo complete" : "Generating demo",
                ($"{bar}  {step * 100 / Steps}%", accent),
                ($"Steps      {step:N0} / {Steps:N0}", normal),
                ($"Elapsed    {total.Elapsed.TotalSeconds:F3} s", normal),
                ($"Speed      {step / elapsed.TotalSeconds:N1} steps/s", normal),
                ("", normal),
                ($"Penguins   {engine.Penguins.Count:N0}", normal),
                ($"Map        Radius {footprint.Radius} · {cells.Length:N0} hexes", normal),
                ($"Frames     {engine.Iterations.Count:N0} in memory", normal),
                ("", normal),
                (completed ? "[A] Analyze    [Q] Quit" : "Calculating steps…", accent));
        }
    }
}
