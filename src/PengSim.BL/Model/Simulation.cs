using System.Collections.ObjectModel;

namespace PengSim.BL.Model;

/// <summary>Owns the environment and penguins for one simulation.</summary>
public sealed class Simulation
{
    private readonly Dictionary<int, Penguin> _penguins = [];

    private readonly List<Environment> _frames = [];

    /// <summary>Creates an empty simulation with the given dimensions.</summary>
    public Simulation(int width, int height)
    {
        _frames.Add(new Environment(width, height));
        Frames = _frames.AsReadOnly();
        Penguins = new ReadOnlyDictionary<int, Penguin>(_penguins);
    }

    /// <summary>Gets the environment that stores penguin positions.</summary>
    public Environment Environment => _frames[^1];

    /// <summary>Occupancy grids in chronological order, starting with initial placement.</summary>
    public IReadOnlyList<Environment> Frames { get; }

    public void Step()
    {
        var next = Environment.Copy();
        // Temporary turn rule: each penguin gets one move in placement order.
        // Replace this loop when the simulation's movement rules are defined.
        foreach (var penguin in _penguins.Values)
        {
            penguin.Move(next);
        }
        _frames.Add(next);
    }

    /// <summary>Gets a live read-only lookup of penguins by occupancy ID.</summary>
    public IReadOnlyDictionary<int, Penguin> Penguins { get; }

    /// <summary>Places a penguin in an empty cell and returns its simulation-local ID.</summary>
    public int AddPenguin(int column, int row)
    {
        if (_frames.Count > 1)
        {
            throw new InvalidOperationException("Place penguins before the first iteration.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Environment.Width);

        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Environment.Height);

        if (Environment[column, row] != Environment.EmptyCell)
        {
            throw new InvalidOperationException("The cell already contains a penguin.");
        }

        var id = _penguins.Count + 1;
        _penguins.Add(id, new Penguin(id, column, row));
        Environment[column, row] = id;

        return id;
    }
}
