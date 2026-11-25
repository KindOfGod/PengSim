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

        for (var zoom = 1; zoom <= MaximumZoomLevel; zoom++)
        {
            var region = GetCoarseRegion(footprint.Region, zoom);
            var (left, right) = region.GetScreenColumns();
            _levels.Add(new Level(region, right - left + 1, new Dictionary<HexCell, int>()));

            var isSingleBucket = region.MinQ == region.MaxQ && region.MinR == region.MaxR;
            if (isSingleBucket)
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
    public void Rebuild(ISimulationStep simulationStep)
    {
        if (!_footprint.Matches(simulationStep.Map))
        {
            throw new ArgumentException("Environment must match the hexagonal footprint.", nameof(simulationStep));
        }

        foreach (var level in _levels)
        {
            level.Counts.Clear();
        }

        _baseCells.Clear();
        foreach (var position in simulationStep.Positions)
        {
            var cell = _footprint.FromMap(position);
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

    /// <inheritdoc />
    public HexRegion GetRegion(int zoom)
    {
        return _levels[zoom - 1].Region;
    }

    /// <inheritdoc />
    /// <remarks>Falls back to the coarsest level when no level fits.</remarks>
    public (int Level, HexRegion Region) FitWorld(int maxColumns, int maxRows)
    {
        var index = _levels.FindIndex(level => level.Columns <= maxColumns && level.Region.RowCount <= maxRows);
        if (index < 0)
        {
            index = _levels.Count - 1;
        }

        return (index + 1, _levels[index].Region);
    }

    /// <summary>
    /// Returns the smallest region at <paramref name="zoom"/> that holds the bucket of every
    /// base cell. Only perimeter cells are converted, because buckets of interior cells
    /// lie between those of the perimeter.
    /// </summary>
    private HexRegion GetCoarseRegion(HexRegion baseRegion, int zoom)
    {
        var bounds = new BucketBounds();
        for (var r = baseRegion.MinR; r <= baseRegion.MaxR; r++)
        {
            var (firstQ, lastQ) = baseRegion.GetRow(r);
            var isTopOrBottomRow = r == baseRegion.MinR || r == baseRegion.MaxR;
            var step = isTopOrBottomRow ? 1 : Math.Max(1, lastQ - firstQ);
            for (var q = firstQ; q <= lastQ; q += step)
            {
                bounds.Include(_scale.ToLevel(new HexCell(q, r), zoom));
            }
        }

        return bounds.ToRegion();
    }

    /// <summary>The bucket counts and screen width of one aggregate zoom level.</summary>
    private readonly record struct Level(HexRegion Region, int Columns, Dictionary<HexCell, int> Counts);

    /// <summary>Collects the Q, R, and S ranges of the buckets passed to <see cref="Include"/>.</summary>
    private sealed class BucketBounds
    {
        private int _minQ = int.MaxValue;
        private int _maxQ = int.MinValue;
        private int _minR = int.MaxValue;
        private int _maxR = int.MinValue;
        private int _minS = int.MaxValue;
        private int _maxS = int.MinValue;

        /// <summary>Widens the ranges so they contain <paramref name="bucket"/>.</summary>
        public void Include(HexCell bucket)
        {
            var s = -bucket.Q - bucket.R;
            _minQ = Math.Min(_minQ, bucket.Q);
            _maxQ = Math.Max(_maxQ, bucket.Q);
            _minR = Math.Min(_minR, bucket.R);
            _maxR = Math.Max(_maxR, bucket.R);
            _minS = Math.Min(_minS, s);
            _maxS = Math.Max(_maxS, s);
        }

        /// <summary>Returns the region described by the collected ranges.</summary>
        public HexRegion ToRegion()
        {
            return new HexRegion(_minQ, _maxQ, _minR, _maxR, _minS, _maxS);
        }
    }
}
