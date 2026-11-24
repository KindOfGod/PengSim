using PengSim.BL.Parameters;

namespace PengSim.BL.Contracts;

/// <summary>Describes everything needed to create and run one simulation.</summary>
/// <remarks>
/// The map is a hexagon whose opposite sides have equal length, so three side
/// lengths describe it fully. Lengths count the cells along a side, including both
/// corner cells. Starting at the upper side and going clockwise, the sides are
/// labeled as A, B, C, A, B, C.
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

    void Validate();

    /// <summary>
    /// Gets the smallest board, in offset columns and rows, that holds the hexagon
    /// with its top side on row 0 and its leftmost cell in column 0. Returns (0, 0)
    /// when any side is zero.
    /// </summary>
    /// <remarks>
    /// Assumes pointy-topped cells with odd rows shifted right by half a cell.
    /// </remarks>
    (uint width, uint height) GridDimensions
    {
        get
        {
            if (SideA == 0 || SideB == 0 || SideC == 0)
            {
                return (0, 0);
            }

            var height = SideB + SideC - 1;
            var spanInHalfCells = 2 * (SideA - 1) + (SideB - 1) + (SideC - 1);
            var leftmostRowShift = (SideC - 1) & 1;
            var width = (spanInHalfCells + leftmostRowShift) / 2 + 1;
            return ((uint)width, (uint)height);
        }
    }
}
