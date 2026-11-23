namespace PengSim.UI.Rendering;

/// <summary>Collects rendering timings and writes the report when disposed.</summary>
internal interface IRenderProfile : IDisposable
{
    /// <summary>Sets the elapsed stopwatch ticks for building the initial heatmap.</summary>
    long Startup { set; }

    /// <summary>Records preparation and output times in stopwatch ticks for one displayed frame.</summary>
    void Record(long preparation, long output);
}
