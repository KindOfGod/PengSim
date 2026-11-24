using System.Collections.ObjectModel;
using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <summary>Read-only positions for one simulation iteration.</summary>
public sealed class SimulationStep : ISimulationStep
{
    private readonly ReadOnlyCollection<PenguinPosition> _positions;

    public IHexMap Map { get; }
    public IReadOnlyList<PenguinPosition> Positions => _positions;

    public ushort? this[PenguinPosition position]
    {
        get
        {
            var index = _positions.IndexOf(position);
            return index < 0 ? null : (ushort)(index + 1);
        }
    }

    internal SimulationStep(IHexMap map, ReadOnlyCollection<PenguinPosition> positions)
    {
        Map = map;
        _positions = positions;
    }
}
