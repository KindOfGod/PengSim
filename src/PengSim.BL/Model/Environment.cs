using System.Collections.ObjectModel;
using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <summary>Read-only positions for one simulation iteration.</summary>
public sealed class Environment : IEnvironment
{
    private readonly ReadOnlyCollection<PenguinPosition> _positions;
    public uint Width { get; }
    public uint Height { get; }
    public IReadOnlyList<PenguinPosition> Positions => _positions;

    public ushort? this[ushort column, ushort row]
    {
        get
        {
            var index = _positions.IndexOf(new PenguinPosition(column, row));
            return index < 0 ? null : (ushort)(index + 1);
        }
    }

    internal Environment(uint width, uint height, ReadOnlyCollection<PenguinPosition> positions)
    {
        Width = width;
        Height = height;
        _positions = positions;
    }
}
