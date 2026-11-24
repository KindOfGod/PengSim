using Autofac;
using PengSim.BL;
using PengSim.BL.Contracts;
using PengSim.BL.Parameters;
using PengSim.UI.Demo;
using PengSim.UI.Map;
using PengSim.UI.Rendering;

var builder = new ContainerBuilder();
builder.RegisterModule<SimulationModule>();
builder.RegisterType<HexGridScale>().As<IHexGridScale>();
builder.RegisterType<HeatmapPalette>().As<IHeatmapPalette>();
builder.RegisterInstance<CreateTerminalBuffer>((width, height) => new TerminalBuffer(width, height));
builder.RegisterType<HexHeatmap>().As<IHexHeatmap>();
builder.RegisterType<MapCamera>().As<IMapCamera>();
builder.RegisterType<FrameRenderer>().As<IFrameRenderer>();
builder.RegisterType<EnvironmentDrawer>().As<IEnvironmentDrawer>();
// Each terminal session disposes its screen and presenter when it exits.
builder.RegisterType<TerminalPresenter>().As<ITerminalPresenter>().ExternallyOwned();
builder.RegisterType<DemoScreen>().As<IDemoScreen>().ExternallyOwned();
builder.RegisterType<SimulationParametersCodec>().As<ISimulationParametersCodec>();
builder.RegisterType<SimulationParameterForm>().As<ISimulationParameterForm>();
builder.RegisterType<SimulationDemo>().As<ISimulationDemo>();

using var container = builder.Build();
container.Resolve<ISimulationDemo>().Run();
