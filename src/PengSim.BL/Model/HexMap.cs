using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <inheritdoc />
public sealed class HexMap : IHexMap
{
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
    public PenguinPosition Center { get; }

    /// <summary>
    /// Creates the map for the given side lengths. Each side needs at least one cell,
    /// and A + C - 2 and B + C - 2 must fit in a ushort coordinate.
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

        // The hexagon is point-symmetric, so its center is the midpoint of the bounding range.
        Center = new PenguinPosition((ushort)(maxQ / 2), (ushort)(maxR / 2));
    }

    /// <inheritdoc />
    public bool Contains(int q, int r)
    {
        // Each pair of opposite sides bounds one axial value: r for A, q for B, and r - q for C.
        var diagonal = r - q;
        return r >= 0 && r <= MaxR
            && q >= 0 && q <= MaxQ
            && diagonal >= -(SideA - 1) && diagonal <= SideB - 1;
    }

    /// <inheritdoc />
    public int Distance(PenguinPosition from, PenguinPosition to)
    {
        var dq = to.Q - from.Q;
        var dr = to.R - from.R;
        return Math.Max(Math.Max(Math.Abs(dq), Math.Abs(dr)), Math.Abs(dq - dr));
    }

    /// <inheritdoc />
    public long SquaredEuclideanDistance(PenguinPosition from, PenguinPosition to)
    {
        // A cell center lies at x = q - r / 2 and y = r * sqrt(3) / 2.
        long dq = to.Q - from.Q;
        long dr = to.R - from.R;
        return dq * dq - dq * dr + dr * dr;
    }

    /// <inheritdoc />
    public IReadOnlyList<PenguinPosition> GetNeighbors(PenguinPosition position)
    {
        var neighbors = new List<PenguinPosition>(NeighborOffsets.Length);
        foreach (var (dq, dr) in NeighborOffsets)
        {
            var q = position.Q + dq;
            var r = position.R + dr;
            if (Contains(q, r))
            {
                neighbors.Add(new PenguinPosition((ushort)q, (ushort)r));
            }
        }

        return neighbors;
    }

    /// <inheritdoc />
    /// <remarks>Draws from the bounding range and retries cells outside the hexagon.</remarks>
    public PenguinPosition GetRandomPosition(Random random)
    {
        while (true)
        {
            var q = random.Next(MaxQ + 1);
            var r = random.Next(MaxR + 1);
            if (Contains(q, r))
            {
                return new PenguinPosition((ushort)q, (ushort)r);
            }
        }
    }

    private static long CountCells(long sideA, long sideB, long sideC)
    {
        var a = sideA - 1;
        var b = sideB - 1;
        var c = sideC - 1;
        return a * b + b * c + c * a + a + b + c + 1;
    }

    private static void ThrowIfCoordinateTooLarge(int largestCoordinate, string expression)
    {
        if (largestCoordinate > ISimulationEngine.MaxCoordinate)
        {
            throw new ArgumentOutOfRangeException(expression, largestCoordinate,
                $"{expression} must be at most {ISimulationEngine.MaxCoordinate}.");
        }
    }
}
