using System.Collections.ObjectModel;
using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <summary>Owns the environment and penguins for one simulation.</summary>
internal sealed class Simulation : ISimulation
{
    private readonly Dictionary<int, IPenguin> _penguins = [];

    private readonly List<IEnvironment> _frames = [];
    private int[,] _cells;

    /// <summary>Occupancy grids in chronological order, starting with initial placement.</summary>
    public IReadOnlyList<IEnvironment> Frames { get; }

    /// <summary>Gets a live read-only lookup of penguins by occupancy ID.</summary>
    public IReadOnlyDictionary<int, IPenguin> Penguins { get; }

    private IEnvironment Environment => _frames[^1];

    /// <summary>Creates an empty simulation with the given dimensions.</summary>
    public Simulation(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _cells = new int[height, width];
        _frames.Add(new Environment(_cells));
        Frames = _frames.AsReadOnly();
        Penguins = new ReadOnlyDictionary<int, IPenguin>(_penguins);
    }

    public void Step()
    {
        var next = (int[,])_cells.Clone();
        // Temporary turn rule: each penguin gets one move in placement order.
        // Replace this loop when the simulation's movement rules are defined.
        foreach (var penguin in _penguins.Values)
        {
            penguin.Move(next);
        }
        _frames.Add(new Environment(next));
        _cells = next;
    }

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

        if (Environment[column, row] != IEnvironment.EmptyCell)
        {
            throw new InvalidOperationException("The cell already contains a penguin.");
        }

        var id = _penguins.Count + 1;
        _penguins.Add(id, new Penguin(id, column, row));
        _cells[row, column] = id;

        return id;
    }
}
