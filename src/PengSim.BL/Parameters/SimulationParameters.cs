using PengSim.BL.Contracts;
using PengSim.BL.Model;

namespace PengSim.BL.Parameters;

/// <summary>Immutable simulation parameters, typically read from JSON.</summary>
public sealed record SimulationParameters : ISimulationParameters
{
    /// <inheritdoc />
    public required ushort SideA { get; init; }

    /// <inheritdoc />
    public required ushort SideB { get; init; }

    /// <inheritdoc />
    public required ushort SideC { get; init; }

    /// <inheritdoc />
    public required ushort PenguinPopulation { get; init; }

    /// <inheritdoc />
    public required PenguinDistribution PenguinDistribution { get; init; }

    /// <inheritdoc />
    public ushort PenguinDistributionSeed { get; init; }

    /// <inheritdoc />
    public required ushort SimulatedSteps { get; init; }

    /// <inheritdoc />
    /// <remarks>Any angle is accepted and normalized, so -90 becomes 270.</remarks>
    public required float WindDirectionDegrees
    {
        get;
        init => field = (value % 360 + 360) % 360;
    }

    /// <inheritdoc />
    public required float WindSpeedMetersPerSecond { get; init; }

    /// <inheritdoc />
    public required float TemperatureCelsius { get; init; }

    /// <summary>Throws if the parameters cannot describe a valid simulation.</summary>
    public void Validate()
    {
        // The map checks the side lengths and coordinate limits.
        IHexMap map = new HexMap(SideA, SideB, SideC);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(PenguinPopulation);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((long)PenguinPopulation, map.CellCount, nameof(PenguinPopulation));
        if (!Enum.IsDefined(PenguinDistribution))
        {
            throw new ArgumentOutOfRangeException(
                nameof(PenguinDistribution), PenguinDistribution, "Unknown distribution.");
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(SimulatedSteps);

        ThrowIfNotFinite(WindDirectionDegrees, nameof(WindDirectionDegrees));
        ThrowIfNotFinite(WindSpeedMetersPerSecond, nameof(WindSpeedMetersPerSecond));
        ArgumentOutOfRangeException.ThrowIfNegative(WindSpeedMetersPerSecond);
        ThrowIfNotFinite(TemperatureCelsius, nameof(TemperatureCelsius));
    }

    private static void ThrowIfNotFinite(float value, string name)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, value, "The value must be a finite number.");
        }
    }
}
