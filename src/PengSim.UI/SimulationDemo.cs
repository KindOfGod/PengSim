using System.Text.RegularExpressions;
using PengSim.BL.Contracts;
using Spectre.Console;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI;

internal sealed class SimulationDemo(ISimulationEngine engine, IAnsiConsole console)
{
    public void Run()
    {
        var simulation = engine.CreateSimulation(width: 5, height: 3);
        simulation.AddPenguin(column: 0, row: 0);
        simulation.AddPenguin(column: 2, row: 1);
        simulation.AddPenguin(column: 3, row: 2);

        console.MarkupLine("[bold aqua]PengSim[/]");
        foreach (string line in DrawGrid(simulation.Environment))
        {
            console.MarkupLine(Regex.Replace(line, @"[0-9]+", "[bold yellow]$0[/]"));
        }

        console.MarkupLine("[yellow]Numbers[/] identify the penguins.");
    }

    private static IEnumerable<string> DrawGrid(HexEnvironment grid)
    {
        const int columnStride = 5;
        const int rowStride = 2;

        var canvas = new char[grid.Height * rowStride + 2][];
        for (int line = 0; line < canvas.Length; line++)
        {
            canvas[line] = new string(' ', grid.Width * columnStride + 2).ToCharArray();
        }

        for (int row = 0; row < grid.Height; row++)
        {
            for (int column = 0; column < grid.Width; column++)
            {
                int left = column * columnStride + 1;
                int top = row * rowStride + column % 2;

                "____".CopyTo(0, canvas[top], left + 1, 4);
                canvas[top + 1][left] = '/';
                canvas[top + 1][left + 5] = '\\';
                "\\____/".CopyTo(0, canvas[top + 2], left, 6);

                int id = grid[column, row];
                if (id != HexEnvironment.EmptyCell)
                {
                    string label = id.ToString();
                    label.CopyTo(0, canvas[top + 1], left + 1 + (4 - label.Length) / 2, label.Length);
                }
            }
        }

        return canvas.Select(line => new string(line).TrimEnd());
    }
}
