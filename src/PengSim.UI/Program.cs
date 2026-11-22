using Autofac;
using PengSim.BL;
using PengSim.UI.Demo;
using PengSim.UI.Rendering;

namespace PengSim.UI;

/// <summary>Configures the console demo dependencies.</summary>
internal static class Program
{
    private static void Main()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<SimulationModule>();
        builder.RegisterType<EnvironmentDrawer>();
        builder.RegisterType<SimulationDemo>();

        using var container = builder.Build();
        container.Resolve<SimulationDemo>().Run();
    }
}
