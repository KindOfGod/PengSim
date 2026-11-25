using System.Globalization;
using PengSim.BL.Contracts;
using PengSim.BL.Model;
using PengSim.BL.Model.Parameters;

namespace PengSim.UI.Demo;

/// <remarks>
/// Each prompt shows its allowed range and repeats until the input is within it.
/// Ranges that depend on earlier answers, such as side C and the population, are
/// computed from those answers.
/// Only the uniform distribution exists, so the form does not ask for it.
/// The placement seed is chosen at random; the parameter code shown in the
/// analysis view keeps it, so a run can still be reproduced.
/// </remarks>
internal sealed class SimulationParameterForm : ISimulationParameterForm
{
    private const string Title = "Simulation from parameters";
    private const int FieldCount = 8;

    // The largest coordinates are A + C - 2 and B + C - 2, and both must fit in a ushort.
    private const int MaxSideSum = ushort.MaxValue + 2;

    public ISimulationParameters? Read(IDemoScreen screen, ISimulationParameters defaults)
    {
        if (AskCount(screen, 1, "Maps top and bottom length", defaults.SideA, ushort.MaxValue) is not { } sideA
            || AskCount(screen, 2, "Maps falling diagonal length", defaults.SideB, ushort.MaxValue) is not { } sideB
            || AskCount(screen, 3, "Maps rising diagonal length", defaults.SideC, MaxSideC(sideA, sideB)) is not { } sideC)
        {
            return null;
        }

        IHexMap map = new HexMap(sideA, sideB, sideC);
        var maxPopulation = (ushort)Math.Min(ushort.MaxValue, map.CellCount);

        if (AskCount(screen, 4, "Number of penguins", defaults.PenguinPopulation, maxPopulation) is not { } population
            || AskCount(screen, 5, "Steps to simulate", defaults.SimulatedSteps, ushort.MaxValue) is not { } steps
            || Ask(screen, 6, "Enter an angle in °", defaults.WindDirectionDegrees,
                float.IsFinite, "Enter a number such as 270 or -90.") is not { } windDirection
            || Ask(screen, 7, "Wind speed in m/s", defaults.WindSpeedMetersPerSecond,
                speed => float.IsFinite(speed) && speed >= 0, "Enter a decimal greater or equal than 0.") is not { } windSpeed
            || Ask(screen, 8, "Temperature in °C", defaults.TemperatureCelsius,
                float.IsFinite, "Enter a number such as -12.5.") is not { } temperature)
        {
            return null;
        }

        return new SimulationParameters
        {
            SideA = sideA,
            SideB = sideB,
            SideC = sideC,
            PenguinPopulation = population,
            PenguinDistribution = PenguinDistribution.Uniform,
            PenguinDistributionSeed = (ushort)Random.Shared.Next(ushort.MaxValue + 1),
            SimulatedSteps = steps,
            WindDirectionDegrees = windDirection,
            WindSpeedMetersPerSecond = windSpeed,
            TemperatureCelsius = temperature,
        };
    }

    /// <summary>Returns the largest side C that keeps both coordinate limits for the given A and B.</summary>
    private static ushort MaxSideC(ushort sideA, ushort sideB)
    {
        return (ushort)Math.Min(ushort.MaxValue, MaxSideSum - Math.Max(sideA, sideB));
    }

    /// <summary>Asks for a whole number from 1 to <paramref name="max"/>, showing that range in the prompt.</summary>
    private static ushort? AskCount(IDemoScreen screen, int fieldNumber, string label, ushort defaultValue, ushort max)
    {
        return Ask(screen, fieldNumber, $"{label} (1–{max})", Math.Min(defaultValue, max),
            value => value >= 1 && value <= max, $"Enter a whole number from 1 to {max}.");
    }

    /// <summary>Repeats the prompt until the input parses and is allowed, or returns null on Escape.</summary>
    private static T? Ask<T>(
        IDemoScreen screen, int fieldNumber, string label, T defaultValue, Func<T, bool> isAllowed, string hint)
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
            if (T.TryParse(input.Trim(), CultureInfo.InvariantCulture, out var value) && isAllowed(value))
            {
                return value;
            }

            text = input;
            error = hint;
        }
    }
}
