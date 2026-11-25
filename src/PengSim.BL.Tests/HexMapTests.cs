using PengSim.BL.Contracts;
using PengSim.BL.Model;

namespace PengSim.BL.Tests;

public sealed class HexMapTests
{
    private const int MaxTestedSide = 6;

    public static TheoryData<ushort, ushort, ushort> SideCombinations()
    {
        var data = new TheoryData<ushort, ushort, ushort>();
        for (ushort a = 1; a <= MaxTestedSide; a++)
        {
            for (ushort b = 1; b <= MaxTestedSide; b++)
            {
                for (ushort c = 1; c <= MaxTestedSide; c++)
                {
                    data.Add(a, b, c);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SideCombinations))]
    public void Contains_matches_cell_count_and_side_lengths(ushort a, ushort b, ushort c)
    {
        IHexMap map = new HexMap(a, b, c);
        var cells = AllCells(map);

        Assert.Equal(map.CellCount, cells.Count);
        Assert.Equal(a, cells.Count(cell => cell.R == 0));
        Assert.Equal(a, cells.Count(cell => cell.R == map.MaxR));
        Assert.Equal(b, cells.Count(cell => cell.Q == 0));
        Assert.Equal(c, cells.Count(cell => cell.Q - cell.R == a - 1));
    }

    [Theory]
    [MemberData(nameof(SideCombinations))]
    public void Corners_and_center_lie_on_the_map(ushort a, ushort b, ushort c)
    {
        IHexMap map = new HexMap(a, b, c);
        (int Q, int R)[] corners =
        [
            (0, 0), (a - 1, 0), (map.MaxQ, c - 1),
            (map.MaxQ, map.MaxR), (c - 1, map.MaxR), (0, b - 1),
        ];

        Assert.All(corners, corner => Assert.True(map.Contains(corner.Q, corner.R)));
        Assert.True(map.Contains(map.Center.Q, map.Center.R));
    }

    [Theory]
    [MemberData(nameof(SideCombinations))]
    public void Distance_equals_breadth_first_steps_from_center(ushort a, ushort b, ushort c)
    {
        IHexMap map = new HexMap(a, b, c);
        var steps = new Dictionary<PenguinPosition, int> { [map.Center] = 0 };
        var queue = new Queue<PenguinPosition>([map.Center]);
        Span<PenguinPosition> buffer = stackalloc PenguinPosition[6];
        while (queue.TryDequeue(out var cell))
        {
            var neighborCount = map.GetNeighbors(cell, buffer);
            foreach (var neighbor in buffer[..neighborCount])
            {
                if (steps.TryAdd(neighbor, steps[cell] + 1))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        Assert.Equal(map.CellCount, steps.Count);
        Assert.All(steps, entry => Assert.Equal(entry.Value, map.Distance(map.Center, entry.Key)));
    }

    [Fact]
    public void Neighbors_are_one_step_and_one_spacing_away()
    {
        IHexMap map = new HexMap(5, 5, 5);
        Span<PenguinPosition> buffer = stackalloc PenguinPosition[6];
        var neighbors = buffer[..map.GetNeighbors(map.Center, buffer)].ToArray();

        Assert.Equal(6, neighbors.Length);
        Assert.All(neighbors, neighbor => Assert.Equal(1, map.Distance(map.Center, neighbor)));
        Assert.All(neighbors, neighbor => Assert.Equal(1, map.SquaredEuclideanDistance(map.Center, neighbor)));
    }

    [Fact]
    public void Too_large_coordinates_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexMap(ushort.MaxValue, 1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexMap(1, 1, 0));
    }

    private static List<PenguinPosition> AllCells(IHexMap map)
    {
        var cells = new List<PenguinPosition>();
        for (var q = 0; q <= map.MaxQ; q++)
        {
            for (var r = 0; r <= map.MaxR; r++)
            {
                if (map.Contains(q, r))
                {
                    cells.Add(new PenguinPosition((ushort)q, (ushort)r));
                }
            }
        }

        return cells;
    }
}
