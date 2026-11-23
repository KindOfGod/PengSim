namespace PengSim.UI.Rendering;

internal interface IRenderProfile : IDisposable
{
    long Startup { set; }
    void Record(long preparation, long output);
}
