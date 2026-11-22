using System.Collections.ObjectModel;

namespace PengSim.BL.Model;

/// <summary>Owns the environment and penguins for one simulation.</summary>
public sealed class Simulation
{
    private readonly Dictionary<int, Penguin> _penguins = [];

    /// <summary>Creates an empty simulation with the given dimensions.</summary>
    public Simulation(int width, int height)
    {
        Environment = new Environment(width, height);
        Penguins = new ReadOnlyDictionary<int, Penguin>(_penguins);
    }

    /// <summary>Gets the environment that stores penguin positions.</summary>
    public Environment Environment { get; }

    /// <summary>Gets a live read-only lookup of penguins by occupancy ID.</summary>
    public IReadOnlyDictionary<int, Penguin> Penguins { get; }

    /// <summary>Places a penguin in an empty cell and returns its simulation-local ID.</summary>
    public int AddPenguin(int column, int row)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Environment.Width);

        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Environment.Height);

        if (Environment[column, row] != Environment.EmptyCell)
        {
            throw new InvalidOperationException("The cell already contains a penguin.");
        }

        var id = _penguins.Count + 1;
        _penguins.Add(id, new Penguin());
        Environment[column, row] = id;

        return id;
    }
}
