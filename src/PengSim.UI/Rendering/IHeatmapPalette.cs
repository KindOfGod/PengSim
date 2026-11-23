namespace PengSim.UI.Rendering;

/// <summary>Provides the colors and glyphs shared by the map and its legend.</summary>
internal interface IHeatmapPalette
{
    /// <summary>Sentinel for the terminal’s default foreground color.</summary>
    const uint DefaultColor = uint.MaxValue;

    /// <summary>RGB accent used for highlights and the world boundary.</summary>
    const uint Accent = 0x5fd7af;

    /// <summary>Gets the glyph and color for cells outside the playable area.</summary>
    TerminalPixel OutsidePaint { get; }

    /// <summary>Gets the glyph and color for the world boundary.</summary>
    TerminalPixel WorldEdgePaint { get; }

    /// <summary>Selects the glyph and color for a bucket with the specified population.</summary>
    TerminalPixel GetDensityPaint(int population);
}
