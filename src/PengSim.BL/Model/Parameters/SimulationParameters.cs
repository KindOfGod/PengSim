using PengSim.BL.Contracts;

namespace PengSim.BL.Model.Parameters;

/// <summary>Immutable simulation parameters, entered in the UI or decoded from a parameter code.</summary>
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

    /// <inheritdoc />
    /// <remarks>The side lengths are checked by creating the <see cref="HexMap"/>.</remarks>
    public void Validate()
    {
        IHexMap mapWithValidSides = new HexMap(SideA, SideB, SideC);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(PenguinPopulation);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((long)PenguinPopulation, mapWithValidSides.CellCount, nameof(PenguinPopulation));
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

    /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> for NaN or infinite values.</summary>
    private static void ThrowIfNotFinite(float value, string name)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, value, "The value must be a finite number.");
        }
    }
}
