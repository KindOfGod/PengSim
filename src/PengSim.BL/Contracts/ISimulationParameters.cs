using PengSim.BL.Parameters;

namespace PengSim.BL.Contracts;

/// <summary>Describes everything needed to create and run one simulation.</summary>
/// <remarks>
/// The map is a hexagon whose opposite sides have equal length, so three side
/// lengths describe it fully. See <see cref="IHexMap"/> for its coordinates.
/// </remarks>
public interface ISimulationParameters
{
    /// <summary>Gets the number of cells along the top and bottom sides.</summary>
    ushort SideA { get; }

    /// <summary>Gets the number of cells along the upper-right and lower-left sides.</summary>
    ushort SideB { get; }

    /// <summary>Gets the number of cells along the upper-left and lower-right sides.</summary>
    ushort SideC { get; }

    /// <summary>Gets the number of penguins placed before the first iteration.</summary>
    ushort PenguinPopulation { get; }

    /// <summary>Gets how penguins are spread across the map before the first iteration.</summary>
    PenguinDistribution PenguinDistribution { get; }

    /// <summary>A seed for reproducing the distribution of the penguins.</summary>
    ushort PenguinDistributionSeed { get; }

    /// <summary>Gets the number of iterations to calculate.</summary>
    ushort SimulatedSteps { get; }

    /// <summary>Gets the direction the wind blows from, in degrees within [0, 360).</summary>
    float WindDirectionDegrees { get; }

    /// <summary>Gets the non-negative wind speed in meters per second.</summary>
    float WindSpeedMetersPerSecond { get; }

    /// <summary>Gets the air temperature in degrees Celsius.</summary>
    float TemperatureCelsius { get; }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if the parameters cannot describe a
    /// valid simulation, for example when the map is too large or the population does not fit.
    /// </summary>
    void Validate();
}
