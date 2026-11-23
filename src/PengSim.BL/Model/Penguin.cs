using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <summary>Represents an individual penguin in a simulation.</summary>
public sealed class Penguin : IPenguin
{
    private static readonly (int Q, int R)[] Directions =
        [(1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1)];

    private int _column;
    private int _row;

    public int Id { get; }

    internal Penguin(int id, int column, int row)
    {
        (Id, _column, _row) = (id, column, row);
    }

    void IPenguin.Move(int[,] cells)
    {
        MoveTowardCenterForDemo(cells);
    }

    /// <summary>Temporary demo rule: move to an available inward neighbor.</summary>
    private void MoveTowardCenterForDemo(int[,] cells)
    {
        var height = cells.GetLength(0);
        var width = cells.GetLength(1);
        var center = HexCell.FromOffset(width / 2, height / 2);
        var current = HexCell.FromOffset(_column, _row);
        var distance = current.DistanceTo(center);
        var destination = (_column, _row);
        var bestScore = long.MaxValue;

        foreach (var (dq, dr) in Directions)
        {
            var neighbor = new HexCell(current.Q + dq, current.R + dr);
            var row = neighbor.R;
            var column = neighbor.Q + (row >> 1);
            if (column < 0 || column >= width || row < 0 || row >= height
                || cells[row, column] != IEnvironment.EmptyCell || neighbor.DistanceTo(center) >= distance)
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
                destination = (column, row);
            }
        }

        if (destination == (_column, _row))
        {
            return;
        }

        cells[_row, _column] = IEnvironment.EmptyCell;
        (_column, _row) = destination;
        cells[_row, _column] = Id;
    }
}
