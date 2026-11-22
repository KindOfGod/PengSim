using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI;

internal sealed class HexHeatmap
{
    private readonly MapViewport _worldBounds;
    private readonly List<Dictionary<Hex, Coverage>> _levels = [];

    public HexHeatmap(HexEnvironment environment, HexFootprint footprint)
    {
        _worldBounds = new MapViewport(0, 0, environment.Width, environment.Height);
        var level = new Dictionary<Hex, Coverage>();
        for (int row = 0; row < environment.Height; row++)
        {
            for (int column = 0; column < environment.Width; column++)
            {
                if (footprint.Contains(column, row))
                {
                    int count = environment[column, row] == HexEnvironment.EmptyCell ? 0 : 1;
                    // Center the parent lattice on the world to preserve its sixfold symmetry.
                    var (q, r) = footprint.ToAxial(column, row);
                    var parent = new Hex(q, r).Parent();
                    Add(level, parent, new Coverage(column, row, column, row, count));
                }
            }
        }

        _levels.Add(level);
        while (level.Count > 1)
        {
            var parents = new Dictionary<Hex, Coverage>();
            foreach (var (hex, coverage) in level)
            {
                Add(parents, hex.Parent(), coverage);
            }

            _levels.Add(parents);
            level = parents;
        }
    }

    public (int Level, MapViewport Bounds, Dictionary<(int Column, int Row), int> Counts) GetView(
        MapViewport viewport, int maxColumns, int maxRows)
    {
        for (int index = 0; index < _levels.Count; index++)
        {
            var level = _levels[index];
            if (viewport == _worldBounds)
            {
                // Enclose the stepped parent boundary in a regular hex at this resolution.
                int radius = level.Keys.Max(hex => HexFootprint.Distance(hex.Q, hex.R));
                int size = radius * 2 + 1;
                if (size > maxColumns || size > maxRows)
                {
                    continue;
                }

                return (index + 1, new MapViewport(-radius, -radius, size, size), DrawWorld(level, radius));
            }

            var counts = new Dictionary<(int Column, int Row), int>();
            foreach (var (hex, coverage) in level)
            {
                if (coverage.Intersects(viewport))
                {
                    counts.Add(hex.ToOffset(), coverage.Count);
                }
            }

            if (counts.Count == 0)
            {
                return (index + 1, default, counts);
            }

            int left = counts.Keys.Min(cell => cell.Column);
            int top = counts.Keys.Min(cell => cell.Row);
            var bounds = new MapViewport(left, top,
                counts.Keys.Max(cell => cell.Column) - left + 1,
                counts.Keys.Max(cell => cell.Row) - top + 1);

            if ((bounds.Width <= maxColumns && bounds.Height <= maxRows) || index == _levels.Count - 1)
            {
                return (index + 1, bounds, counts);
            }
        }

        throw new InvalidOperationException("The heatmap requires at least one level.");
    }

    private static Dictionary<(int Column, int Row), int> DrawWorld(Dictionary<Hex, Coverage> level, int radius)
    {
        var counts = new Dictionary<(int Column, int Row), int>();
        for (int r = -radius; r <= radius; r++)
        {
            int minQ = Math.Max(-radius, -r - radius);
            int maxQ = Math.Min(radius, -r + radius);
            for (int q = minQ; q <= maxQ; q++)
            {
                var hex = new Hex(q, r);
                counts.Add(hex.ToOffset(), level.TryGetValue(hex, out var coverage) ? coverage.Count : 0);
            }
        }

        return counts;
    }

    private static void Add(Dictionary<Hex, Coverage> level, Hex hex, Coverage child)
    {
        level[hex] = level.TryGetValue(hex, out var existing) ? existing.Combine(child) : child;
    }

    private readonly record struct Coverage(int Left, int Top, int Right, int Bottom, int Count)
    {
        public bool Intersects(MapViewport view) =>
            Left < view.Left + view.Width && Right >= view.Left
            && Top < view.Top + view.Height && Bottom >= view.Top;

        public Coverage Combine(Coverage other) => new(
            Math.Min(Left, other.Left), Math.Min(Top, other.Top),
            Math.Max(Right, other.Right), Math.Max(Bottom, other.Bottom),
            Count + other.Count);
    }

    private readonly record struct Hex(int Q, int R)
    {
        // The seven child offsets, indexed by the residue of 3Q + R modulo 7.
        private static readonly Hex[] ChildOffsets =
        [
            new(0, 0), new(0, 1), new(1, -1), new(1, 0),
            new(-1, 0), new(-1, 1), new(0, -1),
        ];

        public (int Column, int Row) ToOffset() => (Q + (R >> 1), R);

        public Hex Parent()
        {
            // Coarse basis vectors (2, 1) and (-1, 3) define an index-7 hex lattice.
            // Removing the child offset makes the inverse transform integral.
            long residue = (3L * Q + R) % 7;
            var offset = ChildOffsets[(int)((residue + 7) % 7)];
            long q = (long)Q - offset.Q;
            long r = (long)R - offset.R;
            return new Hex((int)((3 * q + r) / 7), (int)((2 * r - q) / 7));
        }
    }
}
