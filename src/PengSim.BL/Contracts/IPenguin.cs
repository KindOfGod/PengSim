using PengSim.BL.Model;

namespace PengSim.BL.Contracts;

/// <summary>A penguin in one simulation, which decides where it moves in each step.</summary>
public interface IPenguin
{
    /// <summary>
    /// Gets the penguin's ID, starting at 1. Its position in every <see cref="ISimulationStep"/>
    /// is at index <c>Id - 1</c>.
    /// </summary>
    ushort Id { get; }

    /// <summary>
    /// Returns the cell the penguin moves to from <paramref name="current"/>, or
    /// <paramref name="current"/> when it stays. <paramref name="occupied"/> holds the cells
    /// of all penguins at this point in the step, including <paramref name="current"/>.
    /// </summary>
    internal PenguinPosition Move(PenguinPosition current, IHexMap map, IReadOnlySet<PenguinPosition> occupied);
}
