using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <summary>Represents an individual penguin in a simulation.</summary>
public sealed class Penguin : IPenguin
{
    private static readonly (int Q, int R)[] Directions =
        [(1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1)];

    public int Id { get; }
    private int _column;
    private int _row;

    internal Penguin(int id, int column, int row) =>
        (Id, _column, _row) = (id, column, row);

    /// <summary>Moves into the nearest available inward neighbor, if one exists.</summary>
    void IPenguin.Move(IMutableEnvironment grid)
    {
        var center = HexCell.FromOffset(grid.Width / 2, grid.Height / 2);
        var current = HexCell.FromOffset(_column, _row);
        var distance = current.DistanceTo(center);
        var destination = (_column, _row);
        var bestScore = long.MaxValue;

        foreach (var (dq, dr) in Directions)
        {
            var neighbor = new HexCell(current.Q + dq, current.R + dr);
            var row = neighbor.R;
            var column = neighbor.Q + (row >> 1);
            if (column < 0 || column >= grid.Width || row < 0 || row >= grid.Height
                || grid[column, row] != IEnvironment.EmptyCell || neighbor.DistanceTo(center) >= distance)
            {
                continue;
            }

            // Break ties between shortest hex paths by physical distance to the center.
            long q = neighbor.Q - center.Q, r = row - center.R;
            var score = q * q + q * r + r * r;
            if (score < bestScore)
            {
                bestScore = score;
                destination = (column, row);
            }
        }

        if (destination == (_column, _row)) return;

        grid[_column, _row] = IEnvironment.EmptyCell;
        (_column, _row) = destination;
        grid[_column, _row] = Id;
    }
}
