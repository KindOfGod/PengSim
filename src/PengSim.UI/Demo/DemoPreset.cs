using PengSim.BL.Contracts;
using PengSim.BL.Model.Parameters;

namespace PengSim.UI.Demo;

/// <summary>The built-in demo run, used by the demo menu entry and when input or output is redirected.</summary>
internal static class DemoPreset
{
    private const ushort HexSideLength = 125;
    private const ushort PenguinCount = 500;
    private const ushort IterationCount = 1_000;
    private const float TemperatureCelsius = -20f;

    /// <summary>The display name of the demo run.</summary>
    public const string Name = "Penguin demo";

    /// <summary>Gets the demo parameters. The placement seed is chosen once per process.</summary>
    public static ISimulationParameters Parameters { get; } = new SimulationParameters
    {
        SideA = HexSideLength,
        SideB = HexSideLength,
        SideC = HexSideLength,
        PenguinPopulation = PenguinCount,
        PenguinDistribution = PenguinDistribution.Uniform,
        PenguinDistributionSeed = (ushort)Random.Shared.Next(ushort.MaxValue + 1),
        SimulatedSteps = IterationCount,
        WindDirectionDegrees = 0f,
        WindSpeedMetersPerSecond = 0f,
        TemperatureCelsius = TemperatureCelsius,
    };
}
