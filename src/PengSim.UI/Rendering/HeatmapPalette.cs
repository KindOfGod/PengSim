using Spectre.Console;

namespace PengSim.UI.Rendering;

/// <summary>Defines shared terminal styling and the density scale used by both maps.</summary>
internal static class HeatmapPalette
{
    public const string Accent = "#5fd7af";
    public const string EmptyCellColor = "#767676";

    private const string PenguinColor = "#0087d7";
    /// <summary>Marks the selected minimap region with a high-contrast diamond.</summary>
    public const string ViewportGlyph = $"[#101010 on {Accent}]◈[/]";

    public static readonly Style PanelBorder = Style.Parse(EmptyCellColor);
    public static readonly Style FrameBorder = Style.Parse("#949494");
    public static readonly Style ActiveBorder = Style.Parse(Accent);

    private static readonly (int Maximum, string Color, string Label)[] Bands =
    [
        (0, EmptyCellColor, "0"),
        (1, PenguinColor, "1"),
        (2, "#5f87d7", "2"),
        (4, "#5f5fd7", "3–4"),
        (7, "#875fd7", "5–7"),
        (15, "#af5faf", "8–15"),
        (31, "#d75faf", "16–31"),
        (63, "#d75f87", "32–63"),
        (127, "#d75f5f", "64–127"),
        (int.MaxValue, "#ff0000", "128+"),
    ];

    /// <summary>Gets the minimum width of the swatch-only legend.</summary>
    public static int BandCount => Bands.Length;

    /// <summary>Formats a cell using its density band.</summary>
    public static string DensityGlyph(int count)
    {
        var color = Bands.First(band => count <= band.Maximum).Color;
        var glyph = count == 0 ? "⬡" : "⬢";
        return $"[{color}]{glyph}[/]";
    }

    /// <summary>Formats the most descriptive version of the density legend that fits.</summary>
    public static string DrawScale(int width)
    {
        var fullScale = "[bold]Penguins per hex[/]  "
            + string.Join(" ", Bands.Select(band => $"{DensityGlyph(band.Maximum)}{band.Label}"));

        if (width >= Markup.Remove(fullScale).Length)
        {
            return fullScale;
        }

        var glyphs = string.Concat(Bands.Select(band => DensityGlyph(band.Maximum)));
        var compactScale = $"{Bands[0].Label} {glyphs} {Bands[^1].Label}";

        return width >= Markup.Remove(compactScale).Length ? compactScale : glyphs;
    }
}
