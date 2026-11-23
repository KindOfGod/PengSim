namespace PengSim.UI.Rendering;

/// <summary>Maps population counts to terminal glyphs and foreground colors.</summary>
internal sealed class HeatmapPalette : IHeatmapPalette
{
    public TerminalPixel OutsidePaint { get; } = new('·', 0x444444);
    public TerminalPixel WorldEdgePaint { get; } = new('·', IHeatmapPalette.Accent);

    public TerminalPixel GetDensityPaint(int population)
    {
        foreach (var band in DensityBand.All)
        {
            if (population <= band.Maximum)
            {
                return band.Paint;
            }
        }

        throw new InvalidOperationException("Density bands must cover every population.");
    }
}
