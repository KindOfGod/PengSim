namespace PengSim.BL.Contracts;

/// <summary>Identifies a penguin within one simulation.</summary>
public interface IPenguin
{
    /// <summary>Gets the ID used to represent this penguin in the occupancy grid.</summary>
    int Id { get; }

    /// <summary>Applies one turn to the working grid, indexed by row then column.</summary>
    internal void Move(int[,] cells);
}
