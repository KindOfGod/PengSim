using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI;

internal sealed class HexHeatmap
{
    private readonly List<Dictionary<Hex, Coverage>> _levels = [];

    public HexHeatmap(HexEnvironment environment)
    {
        var level = new Dictionary<Hex, Coverage>();
        for (int row = 0; row < environment.Height; row++)
        {
            for (int column = 0; column < environment.Width; column++)
            {
                int count = environment[column, row] == HexEnvironment.EmptyCell ? 0 : 1;
                var parent = Hex.FromOffset(column, row).Parent();
                Add(level, parent, new Coverage(column, row, column, row, count));
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
            var counts = new Dictionary<(int Column, int Row), int>();
            foreach (var (hex, coverage) in _levels[index])
            {
                if (coverage.Intersects(viewport))
                {
                    counts.Add(hex.ToOffset(), coverage.Count);
                }
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

        public static Hex FromOffset(int column, int row) => new(column - (row >> 1), row);

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
