using System.Collections.ObjectModel;
using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <summary>Read-only positions for one simulation iteration.</summary>
public sealed class Environment : IEnvironment
{
    private readonly ReadOnlyCollection<PenguinPosition> _positions;

    public ushort Width { get; }
    public ushort Height { get; }
    public IReadOnlyList<PenguinPosition> Positions => _positions;

    public ushort this[ushort column, ushort row]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Width);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height);
            var index = _positions.IndexOf(new PenguinPosition(column, row));
            return (ushort)(index + 1);
        }
    }

    internal Environment(ushort width, ushort height, ReadOnlyCollection<PenguinPosition> positions)
    {
        Width = width;
        Height = height;
        _positions = positions;
    }
}
