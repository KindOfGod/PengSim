using System.Globalization;
using System.Numerics;
using PengSim.BL.Contracts;
using PengSim.BL.Model.Parameters;

namespace PengSim.BL.Serialization;

/// <inheritdoc />
/// <remarks>
/// The code holds ten segments joined with <see cref="Delimiter"/>, in this order: the three sides,
/// population, distribution, seed, steps, wind direction, wind speed, and temperature.
/// Whole numbers are written as hex numbers without leading zeros. Floats are written as
/// the hex form of their 32-bit IEEE 754 bits, so they decode to exactly the same value.
/// </remarks>
public sealed class SimulationParametersCodec : ISimulationParametersCodec
{
    /// <summary>Separates the segments. It is not a hex digit, so it cannot appear inside a segment.</summary>
    private const char Delimiter = 'y';

    /// <summary>The number of segments in a valid code.</summary>
    private const int SegmentCount = 10;

    /// <inheritdoc />
    public string Encode(ISimulationParameters parameters)
    {
        string[] segments =
        [
            IntegerToHex(parameters.SideA),
            IntegerToHex(parameters.SideB),
            IntegerToHex(parameters.SideC),
            IntegerToHex(parameters.PenguinPopulation),
            IntegerToHex((ushort)parameters.PenguinDistribution),
            IntegerToHex(parameters.PenguinDistributionSeed),
            IntegerToHex(parameters.SimulatedSteps),
            FloatToHex(parameters.WindDirectionDegrees),
            FloatToHex(parameters.WindSpeedMetersPerSecond),
            FloatToHex(parameters.TemperatureCelsius),
        ];

        return string.Join(Delimiter, segments);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Throws an <see cref="ArgumentException"/> when the segment count is wrong or the decoded
    /// parameters are invalid, and a <see cref="FormatException"/> or <see cref="OverflowException"/>
    /// when a segment is not a hex number that fits its field.
    /// </remarks>
    public ISimulationParameters Decode(string code)
    {
        var segments = code.Split(Delimiter);
        if (segments.Length != SegmentCount)
        {
            throw new ArgumentException(
                $"The code must contain {SegmentCount} segments, but it contains {segments.Length}.", nameof(code));
        }

        var parameters = new SimulationParameters
        {
            SideA = HexToInteger<ushort>(segments[0]),
            SideB = HexToInteger<ushort>(segments[1]),
            SideC = HexToInteger<ushort>(segments[2]),
            PenguinPopulation = HexToInteger<ushort>(segments[3]),
            PenguinDistribution = (PenguinDistribution)HexToInteger<ushort>(segments[4]),
            PenguinDistributionSeed = HexToInteger<ushort>(segments[5]),
            SimulatedSteps = HexToInteger<int>(segments[6]),
            WindDirectionDegrees = HexToFloat(segments[7]),
            WindSpeedMetersPerSecond = HexToFloat(segments[8]),
            TemperatureCelsius = HexToFloat(segments[9]),
        };

        parameters.Validate();
        return parameters;
    }

    /// <summary>Writes a whole number as an uppercase hex number without leading zeros.</summary>
    private static string IntegerToHex<T>(T value) where T : struct, IBinaryInteger<T>
    {
        return value.ToString("X", CultureInfo.InvariantCulture);
    }

    /// <summary>Writes the 32-bit IEEE 754 bits of a float as an uppercase hex number.</summary>
    private static string FloatToHex(float value)
    {
        return BitConverter.SingleToUInt32Bits(value).ToString("X", CultureInfo.InvariantCulture);
    }

    /// <summary>Reads a hex segment written by <see cref="IntegerToHex{T}"/>.</summary>
    private static T HexToInteger<T>(string segment) where T : struct, IBinaryInteger<T>
    {
        return T.Parse(segment, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
    }

    /// <summary>Reads a hex segment written by <see cref="FloatToHex"/>.</summary>
    private static float HexToFloat(string segment)
    {
        var bits = uint.Parse(segment, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        return BitConverter.UInt32BitsToSingle(bits);
    }
}
