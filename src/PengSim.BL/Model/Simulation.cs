using System.Collections.ObjectModel;
using PengSim.BL.Contracts;
using PengSim.BL.Parameters;

namespace PengSim.BL.Model;

/// <summary>Owns the penguins, working occupancy, and recorded positions of one run.</summary>
internal sealed class Simulation : ISimulation
{
    private static readonly ISimulationParametersCodec Codec = new SimulationParametersCodec();

    private readonly IHexMap _map;
    private readonly Dictionary<ushort, IPenguin> _penguins = [];
    private readonly HashSet<PenguinPosition> _occupied = [];
    private readonly List<PenguinPosition> _initialPositions = [];
    private readonly List<IEnvironment> _frames = [];

    public IReadOnlyList<IEnvironment> Frames { get; }
    public IReadOnlyDictionary<ushort, IPenguin> Penguins { get; }
    public ISimulationParameters Parameters { get; }
    public string SimulationCode { get; }

    /// <summary>Creates a simulation from a snapshot of the given parameters and places its penguins.</summary>
    public Simulation(ISimulationParameters parameters)
        : this(parameters, Codec.Encode(parameters))
    {
    }

    /// <summary>Creates a simulation from an encoded parameter code and places its penguins.</summary>
    public Simulation(string code)
        : this(Codec.Decode(code), code)
    {
    }

    private Simulation(ISimulationParameters parameters, string code)
    {
        Parameters = parameters;
        Parameters.Validate();
        SimulationCode = code;
        _map = new HexMap(parameters.SideA, parameters.SideB, parameters.SideC);

        _frames.Add(new Environment(_map, _initialPositions.AsReadOnly()));
        Frames = _frames.AsReadOnly();
        Penguins = new ReadOnlyDictionary<ushort, IPenguin>(_penguins);
        PlacePenguins();
    }

    public void Step()
    {
        var previous = _frames[^1];
        var next = new PenguinPosition[previous.Positions.Count];
        for (var index = 0; index < next.Length; index++)
        {
            var id = (ushort)(index + 1);
            var current = previous.Positions[index];
            var destination = _penguins[id].Move(current, _map, _occupied);
            if (current != destination)
            {
                _occupied.Remove(current);
                _occupied.Add(destination);
            }

            next[index] = destination;
        }

        _frames.Add(new Environment(_map, Array.AsReadOnly(next)));
    }

    public void Exec()
    {
        while (_frames.Count <= Parameters.SimulatedSteps)
        {
            Step();
        }
    }

    /// <summary>
    /// Brute-force placement: draws seeded random cells on the map and keeps those
    /// that are still empty, until the population is placed.
    /// </summary>
    private void PlacePenguins()
    {
        if (Parameters.PenguinDistribution != PenguinDistribution.Uniform)
        {
            throw new NotSupportedException($"Distribution {Parameters.PenguinDistribution} is not implemented.");
        }

        var random = new Random(Parameters.PenguinDistributionSeed);
        while (_penguins.Count < Parameters.PenguinPopulation)
        {
            var position = _map.GetRandomPosition(random);
            if (!_occupied.Contains(position))
            {
                AddPenguin(position);
            }
        }
    }

    private void AddPenguin(PenguinPosition position)
    {
        if (_frames.Count > 1)
        {
            throw new InvalidOperationException("Place penguins before the first iteration.");
        }

        if (_penguins.Count >= ISimulationEngine.MaxPenguins)
        {
            throw new InvalidOperationException($"A simulation supports at most {ISimulationEngine.MaxPenguins} penguins.");
        }

        if (!_map.Contains(position.Q, position.R))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "The cell is not on the map.");
        }

        if (!_occupied.Add(position))
        {
            throw new InvalidOperationException("The cell already contains a penguin.");
        }

        var id = (ushort)(_penguins.Count + 1);
        _penguins.Add(id, new Penguin(id));
        _initialPositions.Add(position);
    }
}
