using PengSim.BL.Model;

namespace PengSim.BL.Contracts;

/// <summary>Identifies a penguin within one simulation.</summary>
public interface IPenguin
{
    /// <summary>Gets the ID used to represent this penguin in the occupancy grid.</summary>
    ushort Id { get; }

    /// <summary>Chooses the position after one turn using the current occupied cells.</summary>
    internal PenguinPosition Move(PenguinPosition current, uint width, uint height, IReadOnlySet<PenguinPosition> occupied);
}
