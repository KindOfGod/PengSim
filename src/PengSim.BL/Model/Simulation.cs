using System.Collections.ObjectModel;
using PengSim.BL.Contracts;
using PengSim.BL.Parameters;

namespace PengSim.BL.Model;

/// <summary>Owns the penguins, working occupancy, and recorded positions of one run.</summary>
internal sealed class Simulation : ISimulation
{
    /// <summary>Decodes parameter codes for <see cref="Simulation(string)"/>.</summary>
    private static readonly ISimulationParametersCodec Codec = new SimulationParametersCodec();

    /// <summary>The map all penguins move on.</summary>
    private readonly IHexMap _map;

    /// <summary>The penguins by ID, starting at 1.</summary>
    private readonly Dictionary<ushort, IPenguin> _penguins = [];

    /// <summary>The cells occupied in the latest frame, updated while a step runs.</summary>
    private readonly HashSet<PenguinPosition> _occupiedCells = [];

    /// <summary>The placement positions, which frame zero shows.</summary>
    private readonly List<PenguinPosition> _initialPositions = [];

    /// <summary>The recorded frames, starting with the initial placement.</summary>
    private readonly List<ISimulationStep> _frames = [];

    /// <inheritdoc />
    public IReadOnlyList<ISimulationStep> Frames { get; }

    /// <inheritdoc />
    public ISimulationParameters Parameters { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<ushort, IPenguin> Penguins { get; }

    /// <summary>
    /// Validates the parameters, creates the map, and places the penguins as frame zero.
    /// Throws an <see cref="ArgumentException"/> when the parameters are invalid.
    /// </summary>
    public Simulation(ISimulationParameters parameters)
    {
        parameters.Validate();
        Parameters = parameters;
        _map = new HexMap(parameters.SideA, parameters.SideB, parameters.SideC);

        _frames.Add(new SimulationStep(_map, _initialPositions.AsReadOnly()));
        Frames = _frames.AsReadOnly();
        Penguins = new ReadOnlyDictionary<ushort, IPenguin>(_penguins);
        PlacePenguins();
    }

    /// <summary>Decodes a parameter code and creates the simulation it describes, like <see cref="Simulation(ISimulationParameters)"/>.</summary>
    public Simulation(string code)
        : this(Codec.Decode(code))
    {
    }

    /// <inheritdoc />
    public void Step()
    {
        var previousPositions = _frames[^1].Positions;
        var nextPositions = new PenguinPosition[previousPositions.Count];
        for (var index = 0; index < nextPositions.Length; index++)
        {
            var id = (ushort)(index + 1);
            var current = previousPositions[index];
            var destination = _penguins[id].Move(current, _map, _occupiedCells);
            if (current != destination)
            {
                _occupiedCells.Remove(current);
                _occupiedCells.Add(destination);
            }

            nextPositions[index] = destination;
        }

        _frames.Add(new SimulationStep(_map, Array.AsReadOnly(nextPositions)));
    }

    /// <inheritdoc />
    public void Exec()
    {
        var recordedSteps = _frames.Count - 1;
        while (recordedSteps < Parameters.SimulatedSteps)
        {
            Step();
            recordedSteps++;
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
            if (!_occupiedCells.Contains(position))
            {
                AddPenguin(position);
            }
        }
    }

    /// <summary>
    /// Adds a penguin with the next ID at <paramref name="position"/> in frame zero.
    /// Throws when a step was already recorded, the population limit is reached,
    /// or the cell is off the map or occupied.
    /// </summary>
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

        if (!_occupiedCells.Add(position))
        {
            throw new InvalidOperationException("The cell already contains a penguin.");
        }

        var id = (ushort)(_penguins.Count + 1);
        _penguins.Add(id, new Penguin(id));
        _initialPositions.Add(position);
    }
}
