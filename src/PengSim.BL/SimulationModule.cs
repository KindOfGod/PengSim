using Autofac;
using PengSim.BL.Contracts;

namespace PengSim.BL;

public sealed class SimulationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SimulationEngine>().As<ISimulationEngine>().SingleInstance();
    }
}
