using PengSim.BL.Model;

namespace PengSim.BL.Contracts;

/// <summary>Stores one run and applies the operations requested by the simulation engine.</summary>
internal interface ISimulation
{
    /// <summary>Gets the position snapshots in iteration order, including initial placement at index zero.</summary>
    IReadOnlyList<IEnvironment> Frames { get; }

    /// <summary>Gets the penguins in this run by occupancy ID.</summary>
    IReadOnlyDictionary<ushort, IPenguin> Penguins { get; }

    /// <summary>Runs one turn for each penguin and records the resulting positions.</summary>
    void Step();

    void Exec();
}
