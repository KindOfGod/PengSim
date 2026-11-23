namespace PengSim.UI.Rendering;

/// <summary>Maps population counts to terminal glyphs and foreground colors.</summary>
internal sealed class HeatmapPalette : IHeatmapPalette
{
    public TerminalPixel OutsidePaint { get; } = new('·', 0x444444);
    public TerminalPixel WorldEdgePaint { get; } = new('·', IHeatmapPalette.Accent);

    // Corresponds to 0, 1, 2, 3-4, 5-7, 8-15, 16-31, 32-63, 64-127,
    // and 128+ penguins respectively. Empty cells use a hollow hex glyph.
    private static readonly TerminalPixel[] Bands =
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

    public ReadOnlySpan<TerminalPixel> DensityBands => Bands;

    /// <summary>
    /// Constant-time band selection for the current zoom bucket.
    /// These paints are immutable and reused for every frame.
    /// </summary>
    public TerminalPixel GetDensityPaint(int population) => Bands[population switch
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
