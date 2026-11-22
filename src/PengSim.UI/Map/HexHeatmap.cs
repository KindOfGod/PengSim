using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Map;

/// <summary>
/// Sparse multiresolution population index over world-aligned hexagon buckets.
/// A bucket coordinate is calculated directly with HexGridScale.ToLevel.
/// Empty buckets require no storage; each population change touches only one
/// bucket at every zoom level.
/// </summary>
internal sealed class HexHeatmap
{
    // One sparse dictionary per zoom level. Only buckets containing penguins
    // have entries; an absent key represents a valid zero population.
    private readonly List<Level> _levels = [];

    public HexHeatmap(HexEnvironment environment, HexFootprint footprint)
    {
        if (environment.Width != footprint.Size || environment.Height != footprint.Size)
        {
            throw new ArgumentException("Environment must match the hexagonal display footprint.", nameof(environment));
        }

        // Build only the world extents, not cell-to-parent membership arrays.
        // Extrema on each of the six world sides take O(radius * levels),
        // instead of visiting every cell at every level.
        // Each step doubles the display hex size. Stop when the full map
        // fits in one bucket; there is no reason to allocate coarser levels.
        const int MaxZoomLevel = 30;
        for (var level = 1; level <= MaxZoomLevel; level++)
        {
            var radius = GetCoarseRadius(footprint.Radius, level);
            _levels.Add(new Level(new HexRegion(default, radius), new Dictionary<HexCell, int>()));
            if (radius == 0)
            {
                break;
            }
        }

        // One initial occupancy scan is necessary with the current Environment
        // API, which exposes an array but no collection of occupied positions.
        // Only *occupied* cells contribute to the sparse aggregation index.
        foreach (var cell in footprint.Region.Cells())
        {
            var (column, row) = footprint.ToOffset(cell);
            if (environment[column, row] != HexEnvironment.EmptyCell)
            {
                ChangePopulation(cell, 1);
            }
        }
    }

    /// <summary>Number of coarse display levels; L0 is the raw hex grid.</summary>
    public int LevelCount => _levels.Count;

    /// <summary>Constant-time population lookup; absent buckets have zero penguins.</summary>
    public int GetPopulation(int level, HexCell cell)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level);
        return _levels[level - 1].Counts.GetValueOrDefault(cell);
    }

    /// <summary>
    /// Apply an occupancy delta when the simulation changes. Invoke this after
    /// each empty-to-occupied (+1) or occupied-to-empty (-1) transition and
    /// before drawing the next frame. A move is a -1 followed by a +1.
    /// Complexity is O(number of zoom levels), independent of world area.
    /// </summary>
    public void ChangePopulation(HexCell baseCell, int delta)
    {
        if (delta is not (1 or -1))
        {
            throw new ArgumentOutOfRangeException(nameof(delta), "Expected +1 or -1.");
        }

        // A base-cell occupancy change affects exactly one coarse hex per
        // level, so this is O(level count), not O(world area).
        for (var i = 0; i < _levels.Count; i++)
        {
            var entry = _levels[i];
            var parent = HexGridScale.ToLevel(baseCell, i + 1);
            var old = entry.Counts.GetValueOrDefault(parent);
            var updated = checked(old + delta);

            if (updated < 0)
            {
                throw new InvalidOperationException("Cannot remove a penguin from an empty hex bucket.");
            }

            if (updated == 0)
            {
                entry.Counts.Remove(parent);
            }
            else
            {
                entry.Counts[parent] = updated;
            }
        }
    }

    public HexRegion GetRegion(int level) => _levels[level - 1].Region;

    /// <summary>
    /// Return the finest complete-world level that fits in a hex of the
    /// requested display radius. The main viewport can subsequently zoom
    /// into any rectangle, including the off-world storage corners.
    /// </summary>
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
    /// Finds the outermost transformed base-world cell on each row.
    /// Every row contributes only two candidates, rather than O(radius)
    /// interior cells. The result is used for the regular display hex.
    /// </summary>
    private static int GetCoarseRadius(int radius, int level)
    {
        var maximum = 0;

        for (var row = -radius; row <= radius; row++)
        {
            var minimumQ = Math.Max(-radius, -row - radius);
            var maximumQ = Math.Min(radius, -row + radius);

            var left = HexGridScale.ToLevel(new HexCell(minimumQ, row), level);
            var right = HexGridScale.ToLevel(new HexCell(maximumQ, row), level);
            maximum = Math.Max(maximum, Math.Max(
                left.DistanceTo(default), right.DistanceTo(default)));
        }

        return maximum;
    }

    private sealed record Level(
        HexRegion Region,
        Dictionary<HexCell, int> Counts);
}
