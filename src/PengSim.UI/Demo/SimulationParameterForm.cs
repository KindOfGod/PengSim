using System.Globalization;
using PengSim.BL.Contracts;
using PengSim.BL.Parameters;

namespace PengSim.UI.Demo;

/// <remarks>
/// Asks for a single side length because the renderer only draws regular hexes.
/// Only the uniform distribution exists, so the form does not ask for it.
/// </remarks>
internal sealed class SimulationParameterForm : ISimulationParameterForm
{
    private const string Title = "Simulation from parameters";
    private const int FieldCount = 7;
    private const string WholeNumberHint = "Enter a whole number from 0 to 65535.";
    private const string DecimalHint = "Enter a number such as -12.5.";

    public ISimulationParameters? Read(IDemoScreen screen, ISimulationParameters defaults)
    {
        if (Ask<ushort>(screen, 1, "Hex side length in cells", defaults.SideA, WholeNumberHint) is not { } side
            || Ask<ushort>(screen, 2, "Number of penguins", defaults.PenguinPopulation, WholeNumberHint) is not { } population
            || Ask<ushort>(screen, 3, "Placement seed", defaults.PenguinDistributionSeed, WholeNumberHint) is not { } seed
            || Ask<ushort>(screen, 4, "Steps to simulate", defaults.SimulatedSteps, WholeNumberHint) is not { } steps
            || Ask<float>(screen, 5, "Wind direction in degrees", defaults.WindDirectionDegrees, DecimalHint) is not { } windDirection
            || Ask<float>(screen, 6, "Wind speed in m/s", defaults.WindSpeedMetersPerSecond, DecimalHint) is not { } windSpeed
            || Ask<float>(screen, 7, "Temperature in °C", defaults.TemperatureCelsius, DecimalHint) is not { } temperature)
        {
            return null;
        }

        return new SimulationParameters
        {
            SideA = side,
            SideB = side,
            SideC = side,
            PenguinPopulation = population,
            PenguinDistribution = PenguinDistribution.Uniform,
            PenguinDistributionSeed = seed,
            SimulatedSteps = steps,
            WindDirectionDegrees = windDirection,
            WindSpeedMetersPerSecond = windSpeed,
            TemperatureCelsius = temperature,
        };
    }

    /// <summary>Repeats the prompt until the input parses, or returns null on Escape.</summary>
    private static T? Ask<T>(IDemoScreen screen, int fieldNumber, string label, T defaultValue, string hint)
        where T : struct, IParsable<T>, IFormattable
    {
        var text = defaultValue.ToString(null, CultureInfo.InvariantCulture);
        string? error = null;
        while (true)
        {
            var input = screen.ReadLine($"{Title} ({fieldNumber}/{FieldCount})", label, text, error);
            if (input is null)
            {
                return null;
            }
            if (T.TryParse(input.Trim(), CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            text = input;
            error = hint;
        }
    }
}
