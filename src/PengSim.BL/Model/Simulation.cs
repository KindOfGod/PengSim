using System.Collections.ObjectModel;
using PengSim.BL.Contracts;
using PengSim.BL.Parameters;

namespace PengSim.BL.Model;

/// <summary>Owns the penguins, working occupancy, and recorded positions of one run.</summary>
internal sealed class Simulation : ISimulation
{
    private readonly ISimulationParametersCodec _codec = new SimulationParametersCodec();
    private readonly Dictionary<ushort, IPenguin> _penguins = [];
    private readonly HashSet<PenguinPosition> _occupied = [];
    private readonly List<PenguinPosition> _initialPositions = [];
    private readonly List<IEnvironment> _frames = [];

    public IReadOnlyList<IEnvironment> Frames { get; }
    public IReadOnlyDictionary<ushort, IPenguin> Penguins { get; }
    public ISimulationParameters Parameters { get; }
    public string SimulationCode { get; }

    /// <summary>Creates an empty simulation from a snapshot of the given parameters.</summary>
    public Simulation(ISimulationParameters parameters)
    {
        Parameters = parameters;
        Parameters.Validate();
        SimulationCode = _codec.Encode(parameters);
        var (width, height) = Parameters.GridDimensions;

        _frames.Add(new Environment(width, height, _initialPositions.AsReadOnly()));
        Frames = _frames.AsReadOnly();
        Penguins = new ReadOnlyDictionary<ushort, IPenguin>(_penguins);
        PlacePenguins();
    }
    
    public Simulation(string code)
    {
        Parameters = _codec.Decode(code);
        Parameters.Validate();
        SimulationCode = code;
        var (width, height) = Parameters.GridDimensions;

        _frames.Add(new Environment(width, height, _initialPositions.AsReadOnly()));
        Frames = _frames.AsReadOnly();
        Penguins = new ReadOnlyDictionary<ushort, IPenguin>(_penguins);
        PlacePenguins();
    }

    private ushort AddPenguin(ushort column, ushort row)
    {
        if (_frames.Count > 1)
        {
            throw new InvalidOperationException("Place penguins before the first iteration.");
        }

        if (_penguins.Count >= ISimulationEngine.MaxPenguins)
        {
            throw new InvalidOperationException($"A simulation supports at most {ISimulationEngine.MaxPenguins} penguins.");
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, _frames[0].Width);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, _frames[0].Height);
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

    public void Exec()
    {
        while (_frames.Count <= Parameters.SimulatedSteps)
        {
            Step();
        }
    }

    /// <summary>
    /// Brute-force placement: draws seeded random cells on the board and keeps those
    /// inside the hexagon that are still empty, until the population is placed.
    /// </summary>
    private void PlacePenguins()
    {
        if (Parameters.PenguinDistribution != PenguinDistribution.Uniform)
        {
            throw new NotSupportedException($"Distribution {Parameters.PenguinDistribution} is not implemented.");
        }

        var random = new Random(Parameters.PenguinDistributionSeed);
        var width = (int)_frames[0].Width;
        var height = (int)_frames[0].Height;
        while (_penguins.Count < Parameters.PenguinPopulation)
        {
            var column = (ushort)random.Next(width);
            var row = (ushort)random.Next(height);
            if (IsInsideHexagon(column, row) && !_occupied.Contains(new PenguinPosition(column, row)))
            {
                AddPenguin(column, row);
            }
        }
    }

    /// <summary>
    /// Checks a board cell against the hexagon laid out by
    /// <see cref="ISimulationParameters.GridDimensions"/>. Each pair of opposite sides
    /// bounds one axial coordinate: r for A, q for B, and q + r for C.
    /// </summary>
    private bool IsInsideHexagon(ushort column, ushort row)
    {
        var cell = HexCell.FromOffset(column, row);
        var sideA = Parameters.SideA;
        var sideB = Parameters.SideB;
        var sideC = Parameters.SideC;

        // Axial q of the top-left corner, given the leftmost cell sits in column 0 on row C - 1.
        var topLeftQ = (sideC - 1) - ((sideC - 1) >> 1);

        var withinRows = cell.R >= 0 && cell.R <= sideB + sideC - 2;
        var withinLeftAndRight = cell.Q >= topLeftQ - (sideC - 1) && cell.Q <= topLeftQ + (sideA - 1);
        var withinDiagonals = cell.Q + cell.R >= topLeftQ && cell.Q + cell.R <= topLeftQ + (sideA - 1) + (sideB - 1);
        return withinRows && withinLeftAndRight && withinDiagonals;
    }
}
