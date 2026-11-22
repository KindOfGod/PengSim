using System.Collections.ObjectModel;

namespace PengSim.BL.Model;

public sealed class Simulation
{
    private readonly Dictionary<int, Penguin> _penguins = new();

    public Simulation(int width, int height)
    {
        Environment = new Environment(width, height);
        Penguins = new ReadOnlyDictionary<int, Penguin>(_penguins);
    }

    public Environment Environment { get; }
    public IReadOnlyDictionary<int, Penguin> Penguins { get; }

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

        int id = _penguins.Count + 1;
        _penguins.Add(id, new Penguin());
        Environment[column, row] = id;
        return id;
    }
}
