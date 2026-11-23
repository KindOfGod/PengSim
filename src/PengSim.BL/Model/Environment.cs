using System.Collections.ObjectModel;
using PengSim.BL.Contracts;

namespace PengSim.BL.Model;

/// <summary>Read-only positions for one simulation iteration.</summary>
public sealed class Environment : IEnvironment
{
    private readonly ReadOnlyCollection<PenguinPosition> _positions;

    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<PenguinPosition> Positions => _positions;

    public ushort this[int column, int row]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(column);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Width);
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Height);
            var index = _positions.IndexOf(new PenguinPosition((ushort)column, (ushort)row));
            return (ushort)(index + 1);
        }
    }

    internal Environment(int width, int height, ReadOnlyCollection<PenguinPosition> positions)
    {
        Width = width;
        Height = height;
        _positions = positions;
    }
}
