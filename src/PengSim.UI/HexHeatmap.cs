using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI;

internal sealed class HexHeatmap
{
    private readonly HexRegion _world;
    private readonly List<Level> _levels = [];

    public HexHeatmap(HexEnvironment environment, HexFootprint footprint)
    {
        _world = footprint.Region;
        var occupied = new HashSet<HexCell>();
        var groups = new Dictionary<HexCell, List<HexCell>>();
        foreach (var cell in _world.Cells())
        {
            var (column, row) = footprint.ToOffset(cell);
            if (environment[column, row] != HexEnvironment.EmptyCell)
            {
                occupied.Add(cell);
            }

            AddChildren(groups, cell.Parent(), [cell]);
        }

        while (true)
        {
            int number = _levels.Count + 1;
            var buckets = new Dictionary<HexCell, Bucket>();
            var counts = new Dictionary<HexCell, int>();
            foreach (var (parent, children) in groups)
            {
                var center = parent.DescendToCenter(number);
                int count = children.Count(occupied.Contains);
                buckets.Add(parent, new Bucket(
                    new HexRegion(center, children.Max(cell => cell.DistanceTo(center))),
                    children,
                    children.MinBy(cell => cell.DistanceTo(center)),
                    count));
                counts.Add(parent, count);
            }

            int radius = groups.Keys.Max(cell => cell.DistanceTo(default));
            _levels.Add(new Level(new HexRegion(default, radius), counts, buckets));
            if (groups.Count == 1)
            {
                break;
            }

            var parents = new Dictionary<HexCell, List<HexCell>>();
            foreach (var (cell, children) in groups)
            {
                AddChildren(parents, cell.Parent(), children);
            }

            groups = parents;
        }
    }

    public HexCell? FindBaseCell(HexCell parent, int level) =>
        _levels[level - 1].Buckets.TryGetValue(parent, out var bucket) ? bucket.BaseCell : null;

    public (int Level, HexRegion Region, IReadOnlyDictionary<HexCell, int> Counts) GetView(
        HexRegion viewport, int maxRadius)
    {
        for (int index = 0; index < _levels.Count; index++)
        {
            var level = _levels[index];
            if (viewport == _world)
            {
                if (level.Region.Radius <= maxRadius)
                {
                    return (index + 1, level.Region, level.Counts);
                }

                continue;
            }

            var center = viewport.Center.Ancestor(index + 1);
            var counts = new Dictionary<HexCell, int>();
            int radius = 0;
            foreach (var (parent, bucket) in level.Buckets)
            {
                if (bucket.Intersects(viewport))
                {
                    radius = Math.Max(radius, parent.DistanceTo(center));
                    counts.Add(parent, bucket.Count);
                }
            }

            if (radius <= maxRadius)
            {
                return (index + 1, new HexRegion(center, radius), counts);
            }
        }

        throw new InvalidOperationException("No parent level fits the viewport.");
    }

    private static void AddChildren(
        Dictionary<HexCell, List<HexCell>> groups, HexCell parent, IEnumerable<HexCell> children)
    {
        if (!groups.TryGetValue(parent, out var group))
        {
            group = [];
            groups.Add(parent, group);
        }

        group.AddRange(children);
    }

    private sealed record Level(
        HexRegion Region, Dictionary<HexCell, int> Counts, Dictionary<HexCell, Bucket> Buckets);

    private sealed record Bucket(HexRegion Bounds, List<HexCell> Children, HexCell BaseCell, int Count)
    {
        public bool Intersects(HexRegion viewport) =>
            Bounds.Center.DistanceTo(viewport.Center) <= Bounds.Radius + viewport.Radius
            && Children.Any(viewport.Contains);
    }
}
