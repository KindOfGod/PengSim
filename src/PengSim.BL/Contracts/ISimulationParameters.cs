using PengSim.BL.Model.Parameters;

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

    /// <summary>Gets the number of penguins placed before the first iteration. At least 1 and at most the map's cell count.</summary>
    ushort PenguinPopulation { get; }

    /// <summary>Gets how penguins are spread across the map before the first iteration.</summary>
    PenguinDistribution PenguinDistribution { get; }

    /// <summary>Gets the random seed for the initial placement, so the same seed gives the same placement.</summary>
    ushort PenguinDistributionSeed { get; }

    /// <summary>Gets the number of steps to calculate after the initial placement. At least 1.</summary>
    ushort SimulatedSteps { get; }

    /// <summary>Gets the direction the wind blows from, in degrees within [0, 360).</summary>
    float WindDirectionDegrees { get; }

    /// <summary>Gets the finite, non-negative wind speed in meters per second.</summary>
    float WindSpeedMetersPerSecond { get; }

    /// <summary>Gets the finite air temperature in degrees Celsius.</summary>
    float TemperatureCelsius { get; }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if the parameters cannot describe a
    /// valid simulation, for example when the map is too large or the population does not fit.
    /// </summary>
    void Validate();
}
