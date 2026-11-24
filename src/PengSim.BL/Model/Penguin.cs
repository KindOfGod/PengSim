using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <inheritdoc />
public sealed class Penguin : IPenguin
{
    /// <inheritdoc />
    public ushort Id { get; }

    /// <summary>Creates a penguin with the given ID, starting at 1.</summary>
    internal Penguin(ushort id)
    {
        Id = id;
    }

    /// <inheritdoc />
    PenguinPosition IPenguin.Move(PenguinPosition current, IHexMap map, IReadOnlySet<PenguinPosition> occupied)
    {
        return MoveTowardCenterForDemo(current, map, occupied);
    }

    /// <summary>
    /// Temporary demo rule: among the free neighbors that are fewer steps from the center,
    /// returns the one with the shortest straight-line distance to it. Returns
    /// <paramref name="position"/> when no such neighbor exists.
    /// </summary>
    private static PenguinPosition MoveTowardCenterForDemo(
        PenguinPosition position, IHexMap map, IReadOnlySet<PenguinPosition> occupied)
    {
        var center = map.Center;
        var stepsToCenter = map.Distance(position, center);
        var destination = position;
        var shortestSquaredDistance = long.MaxValue;

        foreach (var neighbor in map.GetNeighbors(position))
        {
            if (map.Distance(neighbor, center) >= stepsToCenter || occupied.Contains(neighbor))
            {
                continue;
            }

            var squaredDistance = map.SquaredEuclideanDistance(neighbor, center);
            if (squaredDistance >= shortestSquaredDistance)
            {
                continue;
            }

            shortestSquaredDistance = squaredDistance;
            destination = neighbor;
        }

        return destination;
    }
}
