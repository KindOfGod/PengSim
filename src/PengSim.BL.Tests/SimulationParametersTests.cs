using PengSim.BL.Parameters;

namespace PengSim.BL.Tests;

public sealed class SimulationParametersTests
{
    private const int CellsOfHexWithSideThree = 19;

    private static readonly SimulationParameters ValidParameters = new()
    {
        SideA = 3,
        SideB = 3,
        SideC = 3,
        PenguinPopulation = 5,
        PenguinDistribution = PenguinDistribution.Uniform,
        PenguinDistributionSeed = 1,
        SimulatedSteps = 10,
        WindDirectionDegrees = 90f,
        WindSpeedMetersPerSecond = 3f,
        TemperatureCelsius = -15f,
    };

    public static TheoryData<SimulationParameters> InvalidParameters =>
    [
        ValidParameters with { SideA = 0 },
        ValidParameters with { SideB = 0 },
        ValidParameters with { SideC = 0 },
        ValidParameters with { SideA = ushort.MaxValue, SideC = 3 },
        ValidParameters with { PenguinPopulation = 0 },
        ValidParameters with { PenguinPopulation = CellsOfHexWithSideThree + 1 },
        ValidParameters with { PenguinDistribution = (PenguinDistribution)1 },
        ValidParameters with { SimulatedSteps = 0 },
        ValidParameters with { WindSpeedMetersPerSecond = -0.5f },
        ValidParameters with { WindSpeedMetersPerSecond = float.PositiveInfinity },
        ValidParameters with { TemperatureCelsius = float.NaN },
    ];

    [Fact]
    public void Validate_accepts_valid_parameters()
    {
        ValidParameters.Validate();
    }

    [Fact]
    public void Validate_accepts_a_population_that_fills_every_cell()
    {
        var parameters = ValidParameters with { PenguinPopulation = CellsOfHexWithSideThree };

        parameters.Validate();
    }

    [Theory]
    [MemberData(nameof(InvalidParameters))]
    public void Validate_rejects_invalid_parameters(SimulationParameters parameters)
    {
        Assert.ThrowsAny<ArgumentException>(parameters.Validate);
    }

    [Theory]
    [InlineData(-90f, 270f)]
    [InlineData(360f, 0f)]
    [InlineData(450f, 90f)]
    [InlineData(-720f, 0f)]
    public void Wind_direction_is_normalized_to_a_full_turn(float input, float expected)
    {
        var parameters = ValidParameters with { WindDirectionDegrees = input };

        Assert.Equal(expected, parameters.WindDirectionDegrees);
    }
}
