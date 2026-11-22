using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Map;

/// <summary>Builds the density and camera landing cells for each parent level of a static snapshot.</summary>
internal sealed class HexHeatmap
{
    private readonly List<Level> _levels = [];

    /// <summary>Precomputes the hierarchy from the base cells up to the single enclosing parent.</summary>
    public HexHeatmap(HexEnvironment environment, HexFootprint footprint)
    {
        var (occupied, groups) = GroupBaseCells(environment, footprint);

        // Each group carries its base cells forward so counts and landing points stay exact.
        while (true)
        {
            _levels.Add(BuildLevel(_levels.Count + 1, groups, occupied));

            if (groups.Count == 1)
            {
                break;
            }

            groups = GroupParents(groups);
        }
    }

    /// <summary>Finds a playable base cell near the geometric center of the parent, if it exists.</summary>
    public HexCell? FindBaseCell(HexCell parent, int level) =>
        _levels[level - 1].LandingCells.TryGetValue(parent, out var cell) ? cell : null;

    /// <summary>Returns the cached penguin counts keyed by parent coordinates at the given level.</summary>
    public IReadOnlyDictionary<HexCell, int> GetCounts(int level) => _levels[level - 1].Counts;

    /// <summary>Returns the smallest centered hex that encloses the full world at this level.</summary>
    public HexRegion GetRegion(int level) => _levels[level - 1].Region;

    /// <summary>Finds the finest parent level whose full-world hex fits the given radius.</summary>
    public (int Level, HexRegion Region) FitWorld(int maxRadius)
    {
        var index = _levels.FindIndex(level => level.Region.Radius <= maxRadius);

        if (index < 0)
        {
            index = _levels.Count - 1;
        }

        return (index + 1, _levels[index].Region);
    }

    /// <summary>Collects occupied base cells and groups every playable cell under its first parent.</summary>
    private static (HashSet<HexCell> Occupied, Dictionary<HexCell, List<HexCell>> Groups) GroupBaseCells(
        HexEnvironment environment, HexFootprint footprint)
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

        return (occupied, groups);
    }

    /// <summary>Calculates the counts, playable landing points, and world radius for one level.</summary>
    private static Level BuildLevel(
        int number, Dictionary<HexCell, List<HexCell>> groups, HashSet<HexCell> occupied)
    {
        var landingCells = new Dictionary<HexCell, HexCell>();
        var counts = new Dictionary<HexCell, int>();

        foreach (var (parent, children) in groups)
        {
            var center = parent.DescendToCenter(number);
            var count = children.Count(occupied.Contains);

            landingCells.Add(parent, children.MinBy(cell => cell.DistanceTo(center)));
            counts.Add(parent, count);
        }

        var radius = groups.Keys.Max(cell => cell.DistanceTo(default));

        return new Level(new HexRegion(default, radius), counts, landingCells);
    }

    /// <summary>Moves each group of base cells to the next aggregation level.</summary>
    private static Dictionary<HexCell, List<HexCell>> GroupParents(Dictionary<HexCell, List<HexCell>> groups)
    {
        var parents = new Dictionary<HexCell, List<HexCell>>();

        foreach (var (cell, children) in groups)
        {
            AddChildren(parents, cell.Parent(), children);
        }

        return parents;
    }

    /// <summary>Appends base cells to a parent while preserving their enumeration order.</summary>
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

    /// <summary>Stores the rendering and navigation data reused for a parent level.</summary>
    private sealed record Level(
        HexRegion Region, Dictionary<HexCell, int> Counts, Dictionary<HexCell, HexCell> LandingCells);
}
