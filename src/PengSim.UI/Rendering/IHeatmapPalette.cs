namespace PengSim.UI.Rendering;

internal interface IHeatmapPalette
{
    const uint DefaultColor = uint.MaxValue;
    const uint Accent = 0x5fd7af;

    TerminalPixel OutsidePaint { get; }
    TerminalPixel WorldEdgePaint { get; }
    ReadOnlySpan<TerminalPixel> DensityBands { get; }
    TerminalPixel GetDensityPaint(int population);
}
