using System.Globalization;
using PengSim.BL.Contracts;

namespace PengSim.BL.Parameters;

/// <inheritdoc />
/// <remarks>
/// Every field is written as a hex number without leading zeros, and the numbers are
/// joined with <see cref="Delimiter"/>. Floats are stored as fixed-point values with
/// two decimals (<see cref="FixedPointScale"/>), so any further digits are rounded away.
/// To allow negative values, <see cref="SignedOffset"/> is added before storing, giving
/// a range of -327.68 to 327.67. The wind direction is stored within (-180, 180].
/// A missing seed is written as an empty segment.
/// </remarks>
public sealed class SimulationParametersCodec : ISimulationParametersCodec
{
    private const char Delimiter = 'y';

    /// <inheritdoc />
    public string Encode(ISimulationParameters parameters)
    {
        string[] segments =
        [
            Short2Hex(parameters.SideA),
            Short2Hex(parameters.SideB),
            Short2Hex(parameters.SideC),
            Short2Hex(parameters.PenguinPopulation),
            Short2Hex((ushort)parameters.PenguinDistribution),
            Short2Hex(parameters.PenguinDistributionSeed),
            Short2Hex(parameters.SimulatedSteps),
            // Stored as (-180, 180] to fit the range; the setter normalizes it back on decode
            Float2Hex(parameters.WindDirectionDegrees > 180
                ? parameters.WindDirectionDegrees - 360
                : parameters.WindDirectionDegrees),
            Float2Hex(parameters.WindSpeedMetersPerSecond),
            Float2Hex(parameters.TemperatureCelsius),
        ];

        return string.Join(Delimiter, segments);
    }

    /// <inheritdoc />
    public ISimulationParameters Decode(string code)
    {
        var segments = code.Split(Delimiter);
        var expectedNumberOfSegments = 10;

        if (segments.Length < expectedNumberOfSegments)
        {
            throw new ArgumentException($"Hash contains too few segments. Expected {expectedNumberOfSegments}, got {segments.Length}", nameof(code));
        }
        
        var parameters = new SimulationParameters
        {
            SideA = Hex2Short(segments[0]),
            SideB = Hex2Short(segments[1]),
            SideC = Hex2Short(segments[2]),
            PenguinPopulation = Hex2Short(segments[3]),
            PenguinDistribution = (PenguinDistribution)Hex2Short(segments[4]),
            PenguinDistributionSeed = Hex2Short(segments[5]),
            SimulatedSteps = Hex2Short(segments[6]),
            WindDirectionDegrees = Hex2Float(segments[7]),
            WindSpeedMetersPerSecond = Hex2Float(segments[8]),
            TemperatureCelsius = Hex2Float(segments[9]),
        };

        parameters.Validate();
        return parameters;
    }

    private static string Short2Hex(ushort value)
    {
        return value.ToString("X", CultureInfo.InvariantCulture);
    }
    
    private static string Float2Hex(float value)
    {
        return BitConverter.SingleToUInt32Bits(value).ToString("X", CultureInfo.InvariantCulture);
    }
    
    private static ushort Hex2Short(string segment)
    {
        return ushort.Parse(segment, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
    }

    private static float Hex2Float(string segment)
    {
        return BitConverter.UInt32BitsToSingle(
            uint.Parse(segment, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
    }
}
