using Autofac;
using PengSim.BL.Contracts;

namespace PengSim.BL;

/// <summary>Registers simulation services with Autofac.</summary>
public sealed class SimulationModule : Module
{
    /// <summary>Registers one engine instance per container.</summary>
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SimulationEngine>().As<ISimulationEngine>().SingleInstance();
    }
}
