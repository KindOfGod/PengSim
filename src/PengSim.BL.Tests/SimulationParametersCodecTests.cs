using PengSim.BL.Contracts;
using PengSim.BL.Parameters;

namespace PengSim.BL.Tests;

public sealed class SimulationParametersCodecTests
{
    private readonly ISimulationParametersCodec _codec = new SimulationParametersCodec();

    public static TheoryData<SimulationParameters> Parameters =>
    [
        new SimulationParameters
        {
            SideA = 8,
            SideB = 3,
            SideC = 5,
            PenguinPopulation = 12,
            PenguinDistribution = PenguinDistribution.Uniform,
            PenguinDistributionSeed = 42,
            SimulatedSteps = 7,
            WindDirectionDegrees = 271.5f,
            WindSpeedMetersPerSecond = 12.25f,
            TemperatureCelsius = -37.8f,
        },
    ];

    [Theory]
    [MemberData(nameof(Parameters))]
    public void Decode_of_Encode_returns_the_input(SimulationParameters parameters)
    {
        var decoded = _codec.Decode(_codec.Encode(parameters));

        Assert.Equal(parameters, decoded);
    }

    [Theory]
    [MemberData(nameof(Parameters))]
    public void Encode_of_Decode_returns_the_input(SimulationParameters parameters)
    {
        var code = _codec.Encode(parameters);
        
        Assert.Equal(code, _codec.Encode(_codec.Decode(code)));
    }
}
