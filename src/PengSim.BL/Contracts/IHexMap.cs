using PengSim.BL.Model;

namespace PengSim.BL.Contracts;

/// <summary>
/// The hexagonal map of a simulation and all geometry on it, in axial coordinates.
/// </summary>
/// <remarks>
/// The hexagon's bottom-left corner is (0, 0). R counts rows upward from the bottom
/// side. Q counts the top-left-to-bottom-right diagonals to the right, starting with
/// the lower-left side, so every cell has non-negative coordinates. Neighbors differ by
/// (±1, 0), (0, ±1), or ±(1, 1). Side lengths count cells, including both corners, and
/// go clockwise from the top: A, B, C, A, B, C.
/// </remarks>
public interface IHexMap
{
    /// <summary>Gets the number of cells along the top and bottom sides.</summary>
    ushort SideA { get; }

    /// <summary>Gets the number of cells along the upper-right and lower-left sides.</summary>
    ushort SideB { get; }

    /// <summary>Gets the number of cells along the upper-left and lower-right sides.</summary>
    ushort SideC { get; }

    /// <summary>Gets the largest Q of any cell, which is A + C - 2.</summary>
    ushort MaxQ { get; }

    /// <summary>Gets the largest R of any cell, which is B + C - 2.</summary>
    ushort MaxR { get; }

    /// <summary>Gets the number of cells in the hexagon.</summary>
    long CellCount { get; }

    /// <summary>
    /// Gets the cell at the hexagon's geometric center. When the center falls between
    /// cells, gets the neighboring cell with the smaller coordinates.
    /// </summary>
    PenguinPosition Center { get; }

    /// <summary>Checks whether a coordinate pair, which may be negative or too large, lies on the map.</summary>
    bool Contains(int q, int r);

    /// <summary>Returns the number of single-cell steps between two cells.</summary>
    int Distance(PenguinPosition from, PenguinPosition to);

    /// <summary>
    /// Returns the squared straight-line distance between two cell centers, measured in
    /// neighbor spacings, so adjacent cells are 1 apart.
    /// </summary>
    long SquaredEuclideanDistance(PenguinPosition from, PenguinPosition to);

    /// <summary>Returns the up to six neighbors of a cell that lie on the map.</summary>
    IReadOnlyList<PenguinPosition> GetNeighbors(PenguinPosition position);

    /// <summary>Returns a uniformly random cell on the map, drawn from <paramref name="random"/>.</summary>
    PenguinPosition GetRandomPosition(Random random);
}
