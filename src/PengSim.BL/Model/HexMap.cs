using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <inheritdoc />
public sealed class HexMap : IHexMap
{
    /// <summary>The axial (Q, R) offsets from a cell to its six neighbors, counterclockwise from the right.</summary>
    private static readonly (int Q, int R)[] NeighborOffsets =
        [(1, 0), (1, 1), (0, 1), (-1, 0), (-1, -1), (0, -1)];

    /// <inheritdoc />
    public ushort SideA { get; }

    /// <inheritdoc />
    public ushort SideB { get; }

    /// <inheritdoc />
    public ushort SideC { get; }

    /// <inheritdoc />
    public ushort MaxQ { get; }

    /// <inheritdoc />
    public ushort MaxR { get; }

    /// <inheritdoc />
    public long CellCount { get; }

    /// <inheritdoc />
    /// <remarks>The hexagon is point-symmetric, so its center is the midpoint of the Q and R ranges.</remarks>
    public PenguinPosition Center { get; }

    /// <summary>
    /// Creates the map for the given side lengths. Throws an <see cref="ArgumentOutOfRangeException"/>
    /// when a side has no cells or when A + C - 2 or B + C - 2 exceeds <see cref="ISimulationEngine.MaxCoordinate"/>.
    /// </summary>
    public HexMap(ushort sideA, ushort sideB, ushort sideC)
    {
        ArgumentOutOfRangeException.ThrowIfZero(sideA);
        ArgumentOutOfRangeException.ThrowIfZero(sideB);
        ArgumentOutOfRangeException.ThrowIfZero(sideC);
        var maxQ = sideA + sideC - 2;
        var maxR = sideB + sideC - 2;
        ThrowIfCoordinateTooLarge(maxQ, "A + C - 2");
        ThrowIfCoordinateTooLarge(maxR, "B + C - 2");

        SideA = sideA;
        SideB = sideB;
        SideC = sideC;
        MaxQ = (ushort)maxQ;
        MaxR = (ushort)maxR;
        CellCount = CountCells(sideA, sideB, sideC);
        Center = new PenguinPosition((ushort)(maxQ / 2), (ushort)(maxR / 2));
    }

    /// <inheritdoc />
    /// <remarks>Each pair of opposite sides bounds one axial value: R for A, Q for B, and R - Q for C.</remarks>
    public bool Contains(int q, int r)
    {
        var isBetweenSidesA = r >= 0 && r <= MaxR;
        var isBetweenSidesB = q >= 0 && q <= MaxQ;
        var rowMinusDiagonal = r - q;
        var isBetweenSidesC = rowMinusDiagonal >= -(SideA - 1) && rowMinusDiagonal <= SideB - 1;
        return isBetweenSidesA && isBetweenSidesB && isBetweenSidesC;
    }

    /// <inheritdoc />
    public int Distance(PenguinPosition from, PenguinPosition to)
    {
        var deltaQ = to.Q - from.Q;
        var deltaR = to.R - from.R;
        return Math.Max(Math.Max(Math.Abs(deltaQ), Math.Abs(deltaR)), Math.Abs(deltaQ - deltaR));
    }

    /// <inheritdoc />
    /// <remarks>A cell center lies at x = q - r / 2 and y = r * sqrt(3) / 2, which gives dq² - dq·dr + dr².</remarks>
    public long SquaredEuclideanDistance(PenguinPosition from, PenguinPosition to)
    {
        long deltaQ = to.Q - from.Q;
        long deltaR = to.R - from.R;
        return deltaQ * deltaQ - deltaQ * deltaR + deltaR * deltaR;
    }

    /// <inheritdoc />
    public IReadOnlyList<PenguinPosition> GetNeighbors(PenguinPosition position)
    {
        var neighbors = new List<PenguinPosition>(NeighborOffsets.Length);
        foreach (var (offsetQ, offsetR) in NeighborOffsets)
        {
            var q = position.Q + offsetQ;
            var r = position.R + offsetR;
            if (Contains(q, r))
            {
                neighbors.Add(new PenguinPosition((ushort)q, (ushort)r));
            }
        }

        return neighbors;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Draws from the two narrowest of the q, r and r - q ranges and retries cells outside the hexagon.
    /// At least 75% of the draws hit the map in worst case.
    /// </remarks>
    public PenguinPosition GetRandomPosition(Random random)
    {
        var qWidth = MaxQ + 1;
        var rWidth = MaxR + 1;
        var dWidth = SideA + SideB - 1;

        while (true)
        {
            int q;
            int r;
            
            if (dWidth >= qWidth && dWidth >= rWidth)
            {
                q = random.Next(qWidth);
                r = random.Next(rWidth);
            }
            else if (qWidth >= rWidth)
            {
                r = random.Next(rWidth);
                q = r - random.Next(-(SideA - 1), SideB);
            }
            else
            {
                q = random.Next(qWidth);
                r = q + random.Next(-(SideA - 1), SideB);
            }

            if (Contains(q, r))
            {
                return new PenguinPosition((ushort)q, (ushort)r);
            }
        }
    }

    /// <summary>Returns the number of cells in a hexagon with the given side lengths.</summary>
    private static long CountCells(long sideA, long sideB, long sideC)
    {
        var edgesA = sideA - 1;
        var edgesB = sideB - 1;
        var edgesC = sideC - 1;
        return edgesA * edgesB + edgesB * edgesC + edgesC * edgesA + edgesA + edgesB + edgesC + 1;
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> naming <paramref name="expression"/>
    /// when <paramref name="largestCoordinate"/> exceeds <see cref="ISimulationEngine.MaxCoordinate"/>.
    /// </summary>
    private static void ThrowIfCoordinateTooLarge(int largestCoordinate, string expression)
    {
        if (largestCoordinate > ISimulationEngine.MaxCoordinate)
        {
            throw new ArgumentOutOfRangeException(expression, largestCoordinate,
                $"{expression} must be at most {ISimulationEngine.MaxCoordinate}.");
        }
    }
}
