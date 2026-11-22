namespace PengSim.UI.Rendering;

/// <summary>Static, allocation-free color selection for visible hexes.</summary>
internal readonly record struct MapPaint(char Glyph, uint Color);

internal static class HeatmapPalette
{
    public const uint DefaultColor = uint.MaxValue;
    public const uint Accent = 0x5fd7af;

    public static readonly MapPaint OutsidePaint = new('·', 0x444444);
    public static readonly MapPaint WorldEdgePaint = new('·', Accent);

    private static readonly MapPaint[] Bands =
    [
        new('⬡', 0x767676),
        new('⬢', 0x0087d7),
        new('⬢', 0x5f87d7),
        new('⬢', 0x5f5fd7),
        new('⬢', 0x875fd7),
        new('⬢', 0xaf5faf),
        new('⬢', 0xd75faf),
        new('⬢', 0xd75f87),
        new('⬢', 0xd75f5f),
        new('⬢', 0xff0000),
    ];

    public static MapPaint GetDensityPaint(int population) => Bands[population switch
    {
        <= 0 => 0,
        1 => 1,
        2 => 2,
        <= 4 => 3,
        <= 7 => 4,
        <= 15 => 5,
        <= 31 => 6,
        <= 63 => 7,
        <= 127 => 8,
        _ => 9,
    }];
}
