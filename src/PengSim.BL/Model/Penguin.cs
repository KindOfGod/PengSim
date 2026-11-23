using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <summary>Represents an individual penguin in a simulation.</summary>
public sealed class Penguin : IPenguin
{
    private static readonly (int Q, int R)[] Directions =
        [(1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1)];

    public ushort Id { get; }

    internal Penguin(ushort id)
    {
        Id = id;
    }

    PenguinPosition IPenguin.Move(PenguinPosition current, int width, int height, IReadOnlySet<PenguinPosition> occupied)
    {
        return MoveTowardCenterForDemo(current, width, height, occupied);
    }

    /// <summary>Temporary demo rule: move to an available inward neighbor.</summary>
    private static PenguinPosition MoveTowardCenterForDemo(
        PenguinPosition position, int width, int height, IReadOnlySet<PenguinPosition> occupied)
    {
        var center = HexCell.FromOffset(width / 2, height / 2);
        var current = HexCell.FromOffset(position.X, position.Y);
        var distance = current.DistanceTo(center);
        var destination = position;
        var bestScore = long.MaxValue;

        foreach (var (dq, dr) in Directions)
        {
            var neighbor = new HexCell(current.Q + dq, current.R + dr);
            var row = neighbor.R;
            var column = neighbor.Q + (row >> 1);
            if (column < 0 || column >= width || row < 0 || row >= height
                || neighbor.DistanceTo(center) >= distance)
            {
                continue;
            }

            var candidate = new PenguinPosition((ushort)column, (ushort)row);
            if (occupied.Contains(candidate))
            {
                continue;
            }

            // Break ties between shortest hex paths by physical distance to the center.
            var q = (long)neighbor.Q - center.Q;
            var r = (long)row - center.R;
            var score = q * q + q * r + r * r;
            if (score < bestScore)
            {
                bestScore = score;
                destination = candidate;
            }
        }

        return destination;
    }
}
