using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <summary>Represents an individual penguin in a simulation.</summary>
public sealed class Penguin : IPenguin
{
    public ushort Id { get; }

    internal Penguin(ushort id)
    {
        Id = id;
    }

    PenguinPosition IPenguin.Move(PenguinPosition current, IHexMap map, IReadOnlySet<PenguinPosition> occupied)
    {
        return MoveTowardCenterForDemo(current, map, occupied);
    }

    /// <summary>Temporary demo rule: move to an available inward neighbor.</summary>
    private static PenguinPosition MoveTowardCenterForDemo(
        PenguinPosition position, IHexMap map, IReadOnlySet<PenguinPosition> occupied)
    {
        var center = map.Center;
        var distance = map.Distance(position, center);
        var destination = position;
        var bestScore = long.MaxValue;

        foreach (var neighbor in map.GetNeighbors(position))
        {
            if (map.Distance(neighbor, center) >= distance || occupied.Contains(neighbor))
            {
                continue;
            }

            var score = map.SquaredEuclideanDistance(neighbor, center);
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            destination = neighbor;
        }

        return destination;
    }
}
