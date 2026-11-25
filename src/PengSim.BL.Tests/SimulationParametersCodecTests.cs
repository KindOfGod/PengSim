using PengSim.BL.Contracts;
using PengSim.BL.Model.Parameters;
using PengSim.BL.Serialization;

namespace PengSim.BL.Tests;

public sealed class SimulationParametersCodecTests
{
    private const char Delimiter = 'y';
    private const int WindDirectionSegment = 7;
    private const int TemperatureSegment = 9;
    private const string NaNBits = "7FC00000";

    private static readonly SimulationParameters ValidParameters = new()
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
    };

    private readonly ISimulationParametersCodec _codec = new SimulationParametersCodec();

    public static TheoryData<SimulationParameters> Parameters =>
    [
        ValidParameters,
        new SimulationParameters
        {
            SideA = 125,
            SideB = 125,
            SideC = 125,
            PenguinPopulation = 500,
            PenguinDistribution = PenguinDistribution.Uniform,
            PenguinDistributionSeed = 0,
            SimulatedSteps = 1_000,
            WindDirectionDegrees = 0f,
            WindSpeedMetersPerSecond = 0f,
            TemperatureCelsius = -20f,
        },
        ValidParameters with
        {
            SideA = 1,
            SideB = 1,
            SideC = ushort.MaxValue,
            PenguinPopulation = 1,
            PenguinDistributionSeed = ushort.MaxValue,
            SimulatedSteps = ushort.MaxValue,
            WindDirectionDegrees = 359.99f,
        },
        ValidParameters with { WindDirectionDegrees = 0.00001f },
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

    [Fact]
    public void Negative_wind_direction_decodes_as_its_normalized_angle()
    {
        var parameters = ValidParameters with { WindDirectionDegrees = -90f };

        var decoded = _codec.Decode(_codec.Encode(parameters));

        Assert.Equal(270f, decoded.WindDirectionDegrees);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Decode_rejects_a_wrong_segment_count(int extraSegments)
    {
        var segments = _codec.Encode(ValidParameters).Split(Delimiter).ToList();
        if (extraSegments < 0)
        {
            segments.RemoveAt(segments.Count - 1);
        }
        else
        {
            segments.Add("0");
        }

        var code = string.Join(Delimiter, segments);

        Assert.Throws<ArgumentException>(() => _codec.Decode(code));
    }

    [Fact]
    public void Decode_rejects_an_empty_code()
    {
        Assert.Throws<ArgumentException>(() => _codec.Decode(""));
    }

    [Fact]
    public void Decode_rejects_a_segment_that_is_not_hex()
    {
        var code = ReplaceSegment(_codec.Encode(ValidParameters), 0, "zz");

        Assert.Throws<FormatException>(() => _codec.Decode(code));
    }

    [Fact]
    public void Decode_rejects_a_whole_number_that_does_not_fit_a_ushort()
    {
        var code = ReplaceSegment(_codec.Encode(ValidParameters), 0, "10000");

        Assert.Throws<OverflowException>(() => _codec.Decode(code));
    }

    [Fact]
    public void Decode_rejects_a_side_without_cells()
    {
        var code = ReplaceSegment(_codec.Encode(ValidParameters), 0, "0");

        Assert.ThrowsAny<ArgumentException>(() => _codec.Decode(code));
    }

    [Theory]
    [InlineData(WindDirectionSegment)]
    [InlineData(TemperatureSegment)]
    public void Decode_rejects_a_float_that_is_not_finite(int segment)
    {
        var code = ReplaceSegment(_codec.Encode(ValidParameters), segment, NaNBits);

        Assert.ThrowsAny<ArgumentException>(() => _codec.Decode(code));
    }

    [Fact]
    public void Decode_rejects_an_unknown_distribution()
    {
        var code = ReplaceSegment(_codec.Encode(ValidParameters), 4, "1");

        Assert.ThrowsAny<ArgumentException>(() => _codec.Decode(code));
    }

    private static string ReplaceSegment(string code, int index, string segment)
    {
        var segments = code.Split(Delimiter);
        segments[index] = segment;
        return string.Join(Delimiter, segments);
    }
}
