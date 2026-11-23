namespace PengSim.UI.Rendering;

/// <summary>An inclusive upper population limit and its map and legend appearance.</summary>
internal readonly record struct DensityBand(int Maximum, TerminalPixel Paint, string Label)
{
    private static readonly DensityBand[] Bands = CreateBands();

    public static ReadOnlySpan<DensityBand> All => Bands;
    public static int LegendColumnWidth { get; } = Bands.Max(band => band.Label.Length) + 3;

    private static DensityBand[] CreateBands()
    {
        (int Maximum, uint Color)[] thresholds =
        [
            (0, 0x767676), (1, 0x0087d7), (2, 0x5f87d7), (4, 0x5f5fd7),
            (7, 0x875fd7), (15, 0xaf5faf), (31, 0xd75faf), (63, 0xd75f87),
            (127, 0xd75f5f), (int.MaxValue, 0xff0000),
        ];

        var bands = new DensityBand[thresholds.Length];
        for (var i = 0; i < thresholds.Length; i++)
        {
            var (maximum, color) = thresholds[i];
            var minimum = i == 0 ? 0 : thresholds[i - 1].Maximum + 1;
            var label = maximum switch
            {
                int.MaxValue => $"{minimum}+",
                _ when minimum == maximum => $"{minimum}",
                _ => $"{minimum}–{maximum}",
            };
            var paint = new TerminalPixel(maximum == 0 ? '⬡' : '⬢', color);
            bands[i] = new DensityBand(maximum, paint, label);
        }

        return bands;
    }
}
