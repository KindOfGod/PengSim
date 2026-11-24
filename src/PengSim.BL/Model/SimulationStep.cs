using System.Collections.ObjectModel;
using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <inheritdoc />
public sealed class SimulationStep : ISimulationStep
{
    /// <summary>The positions in ID order, kept as a collection so the indexer can search it.</summary>
    private readonly ReadOnlyCollection<PenguinPosition> _positions;

    /// <inheritdoc />
    public IHexMap Map { get; }

    /// <inheritdoc />
    public IReadOnlyList<PenguinPosition> Positions => _positions;

    /// <inheritdoc />
    public ushort? this[PenguinPosition position]
    {
        get
        {
            var index = _positions.IndexOf(position);
            return index < 0 ? null : (ushort)(index + 1);
        }
    }

    /// <summary>Creates a step for the given map that shows <paramref name="positions"/> without copying them.</summary>
    internal SimulationStep(IHexMap map, ReadOnlyCollection<PenguinPosition> positions)
    {
        Map = map;
        _positions = positions;
    }
}
