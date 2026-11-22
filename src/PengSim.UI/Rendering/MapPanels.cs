using System.Text;
using PengSim.UI.Map;
using Spectre.Console;
using Spectre.Console.Rendering;
using HexEnvironment = PengSim.BL.Model.Environment;

namespace PengSim.UI.Rendering;

/// <summary>Renders the main map and the stacked minimap and Information panels.</summary>
internal static class MapPanels
{
    /// <summary>Places the map panels side by side or stacks them in a narrow terminal.</summary>
    public static IRenderable Draw(
        HexEnvironment environment,
        HexFootprint footprint,
        HexHeatmap heatmap,
        MapCamera camera,
        (int Level, HexRegion Region) overview,
        TerminalLayout layout)
    {
        var main = DrawMain(environment, footprint, heatmap, camera, overview, layout);
        var (mini, minimapHeight) = DrawMinimap(heatmap, camera, overview, layout);
        var information = DrawInformation(mini.Width, layout.SideHeight - minimapHeight);
        var side = new Rows(mini, information);

        if (!layout.SideBySide)
        {
            return new Rows(main, side);
        }

        var grid = new Grid().AddColumns(
            new GridColumn { Width = main.Width, Padding = new Padding(0, 0) },
            new GridColumn { Width = 1, Padding = new Padding(0, 0) },
            new GridColumn { Width = mini.Width, Padding = new Padding(0, 0) });

        grid.AddRow(main, new Text(" "), side);
        return grid;
    }

    /// <summary>Draws the overview, a parent-hex heatmap, or actual cells at L0.</summary>
    private static Panel DrawMain(
        HexEnvironment environment,
        HexFootprint footprint,
        HexHeatmap heatmap,
        MapCamera camera,
        (int Level, HexRegion Region) overview,
        TerminalLayout layout)
    {
        var level = camera.Level ?? overview.Level;
        var view = camera.Level.HasValue
            ? new HexRegion(camera.Center.Ancestor(level), layout.DetailRadius)
            : overview.Region;
        var drawRegion = view with { Radius = layout.MainRadius };
        var counts = level > 0 ? heatmap.GetCounts(level) : null;
        var world = level > 0 ? heatmap.GetRegion(level) : footprint.Region;
        var title = level == 0 ? "Hex grid · L0" : $"Heatmap · L{level}";

        return DrawHexPanel(title, MapBounds.FromHex(drawRegion), layout.MainWidth, cell =>
        {
            if (!drawRegion.Contains(cell))
            {
                return " ";
            }

            var distance = cell.DistanceTo(view.Center);
            if (distance > view.Radius)
            {
                return distance == view.Radius + 1 ? $"[{HeatmapPalette.Accent}]·[/]" : " ";
            }

            if (!world.Contains(cell))
            {
                return $"[{HeatmapPalette.EmptyCellColor}]·[/]";
            }

            if (counts is not null)
            {
                return HeatmapPalette.DensityGlyph(counts.GetValueOrDefault(cell));
            }

            var (column, row) = footprint.ToOffset(cell);
            return HeatmapPalette.DensityGlyph(environment[column, row] == HexEnvironment.EmptyCell ? 0 : 1);
        });
    }

    /// <summary>Draws the full world and fits the panel to any marker that extends past the edge.</summary>
    private static (Panel Panel, int Height) DrawMinimap(
        HexHeatmap heatmap, MapCamera camera, (int Level, HexRegion Region) overview, TerminalLayout layout)
    {
        var bounds = MapBounds.FromHex(overview.Region);
        HexRegion? marker = null;

        if (camera.Level is int level)
        {
            var largestMarker = MarkerRadius(layout.DetailRadius, overview.Level - 1, overview.Level, layout.MiniRadius);
            var margin = Math.Min(largestMarker, layout.MiniRadius - overview.Region.Radius);
            var radius = MarkerRadius(layout.DetailRadius, level, overview.Level, margin);

            marker = new HexRegion(camera.Center.Ancestor(overview.Level), radius);
            bounds = bounds.Union(MapBounds.FromHex(marker.Value));
        }

        var counts = heatmap.GetCounts(overview.Level);
        var panel = DrawHexPanel($"Minimap · L{overview.Level}", bounds, layout.MiniWidth, cell =>
        {
            if (marker is { } viewport && viewport.Contains(cell))
            {
                return HeatmapPalette.ViewportGlyph;
            }

            return overview.Region.Contains(cell) ? HeatmapPalette.DensityGlyph(counts.GetValueOrDefault(cell)) : " ";
        });

        return (panel, bounds.Height + 2);
    }

    /// <summary>Projects a view radius to the minimap and caps it at the reserved margin.</summary>
    private static int MarkerRadius(int radius, int level, int miniLevel, int margin)
    {
        // A parent covers seven children, so its linear scale grows by the square root of seven.
        var scale = Math.Pow(7, (level - miniLevel) / 2.0);
        return Math.Min(margin, (int)Math.Round(radius * scale));
    }

    /// <summary>Reserves the empty space beneath the minimap for simulation information.</summary>
    private static Panel DrawInformation(int? width, int height)
    {
        var rows = Math.Max(1, height - 2);
        var contents = string.Join("\n", Enumerable.Repeat(" ", rows));

        return new Panel(new Text(contents)) { Width = width, BorderStyle = HeatmapPalette.PanelBorder }
            .Header("Information")
            .Border(BoxBorder.Rounded);
    }

    /// <summary>Draws a projected hex lattice with the supplied glyph for each cell.</summary>
    private static Panel DrawHexPanel(string title, MapBounds bounds, int maxWidth, Func<HexCell, string> glyph)
    {
        var text = new StringBuilder();

        for (var row = bounds.Top; row <= bounds.Bottom; row++)
        {
            if (row > bounds.Top)
            {
                text.Append('\n');
            }

            for (var x = bounds.Left; x <= bounds.Right; x++)
            {
                // Only alternating columns are cell centers on each staggered row.
                var axialColumn = x - row;
                text.Append((axialColumn & 1) == 0 ? glyph(new HexCell(axialColumn / 2, row)) : " ");
            }
        }

        var width = Math.Min(maxWidth, Math.Max(title.Length + 4, bounds.Width + 4));
        return new Panel(new Markup(text.ToString())) { Width = width, BorderStyle = HeatmapPalette.PanelBorder }
            .Header(title)
            .Border(BoxBorder.Rounded);
    }

    /// <summary>Describes the screen-aligned bounds of a projected hex region.</summary>
    private readonly record struct MapBounds(int Left, int Top, int Right, int Bottom)
    {
        public int Width => Right - Left + 1;

        public int Height => Bottom - Top + 1;

        /// <summary>Projects a hexagon to screen coordinates with a one-column offset per row.</summary>
        public static MapBounds FromHex(HexRegion hex)
        {
            var x = 2 * hex.Center.Q + hex.Center.R;
            var radius = hex.Radius;

            return new MapBounds(x - 2 * radius, hex.Center.R - radius, x + 2 * radius, hex.Center.R + radius);
        }

        /// <summary>Returns the smallest screen rectangle containing both bounds.</summary>
        public MapBounds Union(MapBounds other) =>
            new(
                Math.Min(Left, other.Left),
                Math.Min(Top, other.Top),
                Math.Max(Right, other.Right),
                Math.Max(Bottom, other.Bottom));
    }
}
