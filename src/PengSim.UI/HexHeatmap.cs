using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI;

internal sealed class HexHeatmap
{
    private readonly HexRegion _world;
    private readonly List<Dictionary<HexCell, int>> _levels = [];

    public HexHeatmap(HexEnvironment environment, HexFootprint footprint)
    {
        _world = footprint.Region;
        var level = new Dictionary<HexCell, int>();
        foreach (var cell in _world.Cells())
        {
            var (column, row) = footprint.ToOffset(cell);
            level.Add(cell, environment[column, row] == HexEnvironment.EmptyCell ? 0 : 1);
        }

        _levels.Add(level);
        do
        {
            var parents = new Dictionary<HexCell, int>();
            foreach (var (cell, count) in level)
            {
                var parent = cell.Parent();
                parents[parent] = parents.GetValueOrDefault(parent) + count;
            }

            _levels.Add(parents);
            level = parents;
        } while (level.Count > 1);
    }

    public (int Level, HexRegion Region, IReadOnlyDictionary<HexCell, int> Counts) GetView(
        HexRegion viewport, int maxRadius)
    {
        if (viewport == _world)
        {
            for (int level = 1; level < _levels.Count; level++)
            {
                var counts = _levels[level];
                int radius = counts.Keys.Max(cell => cell.DistanceTo(default));
                if (radius <= maxRadius)
                {
                    return (level, new HexRegion(default, radius), counts);
                }
            }
        }

        var cells = viewport.Cells().ToHashSet();
        var center = viewport.Center;
        for (int level = 1; level < _levels.Count; level++)
        {
            center = center.Parent();
            cells = cells.Select(cell => cell.Parent()).ToHashSet();
            int radius = cells.Max(cell => cell.DistanceTo(center));
            if (radius > maxRadius && level < _levels.Count - 1)
            {
                continue;
            }

            var counts = new Dictionary<HexCell, int>();
            foreach (var cell in cells)
            {
                if (_levels[level].TryGetValue(cell, out int count))
                {
                    counts.Add(cell, count);
                }
            }

            return (level, new HexRegion(center, radius), counts);
        }

        throw new InvalidOperationException("The heatmap requires a parent level.");
    }

}
