using HexCell = PengSim.BL.Model.HexCell;
using PengSim.BL.Contracts;

namespace PengSim.UI.Map;

/// <summary>
/// Population counts for the current simulation iteration, grouped into
/// world-aligned hexagonal zoom levels. Geometry is fixed; counts can be
/// rebuilt once after each simulation step.
/// </summary>
internal sealed class HexHeatmap : IHexHeatmap
{
    private const int MaximumZoomLevel = 30;

    private readonly IHexGridScale _scale;
    private readonly HexFootprint _footprint;
    private readonly HashSet<HexCell> _baseCells = [];
    private readonly List<Level> _levels = [];

    public HexHeatmap(HexFootprint footprint, IHexGridScale scale)
    {
        _footprint = footprint;
        _scale = scale;

        // World size and bucket coordinates never change between iterations.
        // Only keep the enclosing hex for each zoom level, not every base cell.
        for (var zoom = 1; zoom <= MaximumZoomLevel; zoom++)
        {
            var radius = GetCoarseRadius(footprint.Radius, zoom);
            _levels.Add(new Level(new HexRegion(default, radius), new Dictionary<HexCell, int>()));

            if (radius == 0)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Replace density counts with one snapshot of the current environment.
    /// Reuse the dictionaries to avoid allocations on every iteration.
    /// Navigation and zooming only read these counts; they never rebuild them.
    /// </summary>
    public void Rebuild(IEnvironment environment)
    {
        if (environment.Width != _footprint.Size || environment.Height != _footprint.Size)
        {
            throw new ArgumentException("Environment must match the hexagonal footprint.", nameof(environment));
        }

        foreach (var level in _levels)
        {
            level.Counts.Clear();
        }

        _baseCells.Clear();
        foreach (var position in environment.Positions)
        {
            var cell = _footprint.ToAxial(position.X, position.Y);
            if (!_footprint.Region.Contains(cell))
            {
                continue;
            }

            _baseCells.Add(cell);
            for (var zoom = 1; zoom <= _levels.Count; zoom++)
            {
                var bucket = _scale.ToLevel(cell, zoom);
                var counts = _levels[zoom - 1].Counts;
                counts[bucket] = counts.GetValueOrDefault(bucket) + 1;
            }
        }
    }

    /// <summary>Absent entries represent empty buckets.</summary>
    public int GetPopulation(int zoom, HexCell cell)
    {
        if (zoom == 0)
        {
            return _baseCells.Contains(cell) ? 1 : 0;
        }

        return _levels[zoom - 1].Counts.GetValueOrDefault(cell);
    }

    public HexRegion GetRegion(int zoom) => _levels[zoom - 1].Region;

    /// <summary>Use the finest level whose entire hexagonal world fits.</summary>
    public (int Level, HexRegion Region) FitWorld(int maxRadius)
    {
        var index = _levels.FindIndex(level => level.Region.Radius <= maxRadius);
        if (index < 0)
        {
            index = _levels.Count - 1;
        }

        return (index + 1, _levels[index].Region);
    }

    /// <summary>
    /// Find the enclosing coarse-world radius from the two ends of each
    /// hexagonal row. Interior points cannot extend its outer radius.
    /// </summary>
    private int GetCoarseRadius(int radius, int zoom)
    {
        var result = 0;
        for (var row = -radius; row <= radius; row++)
        {
            var left = new HexCell(Math.Max(-radius, -row - radius), row);
            var right = new HexCell(Math.Min(radius, -row + radius), row);
            result = Math.Max(result, Math.Max(
                _scale.ToLevel(left, zoom).DistanceTo(default),
                _scale.ToLevel(right, zoom).DistanceTo(default)));
        }

        return result;
    }

    private readonly record struct Level(HexRegion Region, Dictionary<HexCell, int> Counts);
}
