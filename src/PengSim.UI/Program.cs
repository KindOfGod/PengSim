using Autofac;
using PengSim.BL;
using PengSim.UI.Demo;
using PengSim.UI.Rendering;
using Spectre.Console;

namespace PengSim.UI;

/// <summary>Configures the console demo dependencies.</summary>
internal static class Program
{
    /// <summary>Builds the container and runs the demo.</summary>
    private static void Main()
    {
        var builder = new ContainerBuilder();

        builder.RegisterModule<SimulationModule>();
        builder.RegisterInstance(AnsiConsole.Console).As<IAnsiConsole>().ExternallyOwned();
        builder.RegisterType<EnvironmentDrawer>();
        builder.RegisterType<SimulationDemo>();

        using var container = builder.Build();

        container.Resolve<SimulationDemo>().Run();
    }
}
