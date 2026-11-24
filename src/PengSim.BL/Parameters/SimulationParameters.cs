using PengSim.BL.Contracts;

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

    /// <summary>Gets the number of playable cells in the hexagon.</summary>
    private int CellCount
    {
        get
        {
            int a = SideA - 1, b = SideB - 1, c = SideC - 1;
            if (a < 0 || b < 0 || c < 0)
            {
                return 0;
            }
            return a * b + b * c + c * a + a + b + c + 1;
        }
    }

    /// <summary>Throws if the parameters cannot describe a valid simulation.</summary>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(SideA);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(SideB);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(SideC);
        
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(PenguinPopulation);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((long)PenguinPopulation, CellCount, nameof(PenguinPopulation));
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
