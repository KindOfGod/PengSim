namespace PengSim.UI.Rendering;

/// <summary>
    /// One already-selected glyph and packed RGB foreground. The UI never
    /// constructs per-hex Spectre styles or markup strings.
    /// </summary>
internal readonly record struct MapPaint(char Glyph, uint Color);

/// <summary>
    /// Maps penguin counts to one of ten static heatmap bands. 0xRRGGBB
    /// denotes packed 24-bit red/green/blue, not a terminal escape sequence.
    /// The ANSI encoding happens only in the terminal rendering layer.
    /// </summary>
internal static class HeatmapPalette
{
    // Outside the valid 24-bit RGB range: tells ANSI output to restore the
    // terminal's default foreground instead of selecting a specific color.
    public const uint DefaultColor = uint.MaxValue;
    public const uint Accent = 0x5fd7af;

    public static readonly MapPaint OutsidePaint = new('·', 0x444444);
    public static readonly MapPaint WorldEdgePaint = new('·', Accent);

    // Corresponds to 0, 1, 2, 3-4, 5-7, 8-15, 16-31, 32-63, 64-127,
    // and 128+ penguins respectively. Empty cells use a hollow hex glyph.
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

    /// <summary>
    /// Constant-time band selection for the current zoom bucket.
    /// These paints are immutable and reused for every frame.
    /// </summary>
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
