using System.Collections.ObjectModel;
using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <summary>Owns the penguins, working occupancy, and recorded positions of one run.</summary>
internal sealed class Simulation : ISimulation
{
    private readonly Dictionary<ushort, IPenguin> _penguins = [];
    private readonly HashSet<PenguinPosition> _occupied = [];
    private readonly List<PenguinPosition> _initialPositions = [];
    private readonly List<IEnvironment> _frames = [];

    public IReadOnlyList<IEnvironment> Frames { get; }
    public IReadOnlyDictionary<ushort, IPenguin> Penguins { get; }

    public Simulation(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, (int)ISimulationEngine.MaxDimension);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, (int)ISimulationEngine.MaxDimension);

        _frames.Add(new Environment(width, height, _initialPositions.AsReadOnly()));
        Frames = _frames.AsReadOnly();
        Penguins = new ReadOnlyDictionary<ushort, IPenguin>(_penguins);
    }

    public ushort AddPenguin(ushort column, ushort row)
    {
        if (_frames.Count > 1)
        {
            throw new InvalidOperationException("Place penguins before the first iteration.");
        }

        if (_penguins.Count >= ISimulationEngine.MaxPenguins)
        {
            throw new InvalidOperationException($"A simulation supports at most {ISimulationEngine.MaxPenguins} penguins.");
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((int)column, _frames[0].Width, nameof(column));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((int)row, _frames[0].Height, nameof(row));
        var position = new PenguinPosition(column, row);
        if (!_occupied.Add(position))
        {
            throw new InvalidOperationException("The cell already contains a penguin.");
        }

        var id = (ushort)(_penguins.Count + 1);
        _penguins.Add(id, new Penguin(id));
        _initialPositions.Add(position);
        return id;
    }

    public void Step()
    {
        var previous = _frames[^1];
        var next = new PenguinPosition[previous.Positions.Count];
        for (var index = 0; index < next.Length; index++)
        {
            var id = (ushort)(index + 1);
            var current = previous.Positions[index];
            var destination = _penguins[id].Move(current, previous.Width, previous.Height, _occupied);
            if (current != destination)
            {
                _occupied.Remove(current);
                _occupied.Add(destination);
            }

            next[index] = destination;
        }

        _frames.Add(new Environment(previous.Width, previous.Height, Array.AsReadOnly(next)));
    }
}
