using Spectre.Console;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI;

internal sealed class EnvironmentDrawer(IAnsiConsole console)
{
    public void Draw(HexEnvironment environment)
    {
        var rows = new string[environment.Height];
        for (int row = 0; row < environment.Height; row++)
        {
            var cells = new string[environment.Width];
            for (int column = 0; column < environment.Width; column++)
            {
                cells[column] = environment[column, row] == HexEnvironment.EmptyCell
                    ? "⬡"
                    : "[blue]⬢[/]";
            }

            string indent = row % 2 == 0 ? "" : " ";
            rows[row] = indent + string.Join(" ", cells);
        }

        console.Write(new Panel(new Markup(string.Join("\n", rows)))
            .Header("PengSim")
            .Border(BoxBorder.Rounded));
    }
}
