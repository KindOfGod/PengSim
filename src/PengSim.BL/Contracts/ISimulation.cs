namespace PengSim.BL.Contracts;

/// <summary>Stores one run and applies the operations requested by the simulation engine.</summary>
internal interface ISimulation
{
    /// <summary>Gets the grids in iteration order, including initial placement at index zero.</summary>
    IReadOnlyList<IEnvironment> Frames { get; }

    /// <summary>Gets the penguins in this run by occupancy ID.</summary>
    IReadOnlyDictionary<int, IPenguin> Penguins { get; }

    /// <summary>Places a penguin before stepping and returns its assigned ID.</summary>
    int AddPenguin(int column, int row);

    /// <summary>Runs one turn for each penguin and records the resulting grid.</summary>
    void Step();
}
