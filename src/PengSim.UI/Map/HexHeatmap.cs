using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Map;

internal sealed class HexHeatmap
{
    private readonly List<Level> _levels = [];

    public HexHeatmap(HexEnvironment environment, HexFootprint footprint)
    {
        var occupied = new HashSet<HexCell>();
        var groups = new Dictionary<HexCell, List<HexCell>>();
        foreach (var cell in footprint.Region.Cells())
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
            var landingCells = new Dictionary<HexCell, HexCell>();
            var counts = new Dictionary<HexCell, int>();
            foreach (var (parent, children) in groups)
            {
                var center = parent.DescendToCenter(number);
                int count = children.Count(occupied.Contains);
                landingCells.Add(parent, children.MinBy(cell => cell.DistanceTo(center)));
                counts.Add(parent, count);
            }

            int radius = groups.Keys.Max(cell => cell.DistanceTo(default));
            _levels.Add(new Level(new HexRegion(default, radius), counts, landingCells));
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
        _levels[level - 1].LandingCells.TryGetValue(parent, out var cell) ? cell : null;

    public IReadOnlyDictionary<HexCell, int> GetCounts(int level) => _levels[level - 1].Counts;

    public HexRegion GetRegion(int level) => _levels[level - 1].Region;

    public (int Level, HexRegion Region) FitWorld(int maxRadius)
    {
        int index = _levels.FindIndex(level => level.Region.Radius <= maxRadius);
        if (index < 0)
        {
            index = _levels.Count - 1;
        }

        return (index + 1, _levels[index].Region);
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
        HexRegion Region, Dictionary<HexCell, int> Counts, Dictionary<HexCell, HexCell> LandingCells);
}
