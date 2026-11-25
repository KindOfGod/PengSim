namespace PengSim.UI.Map;

/// <summary>Controls the center and zoom level of the map view.</summary>
internal interface IMapCamera
{
    /// <summary>Gets the camera center in base-grid axial coordinates.</summary>
    HexCell Center { get; }

    /// <summary>Gets the detail level, or null for the full-world overview.</summary>
    int? Level { get; }

    /// <summary>Moves by the requested number of cells at the given display level.</summary>
    void Pan(int horizontal, int vertical, int level, int steps);

    /// <summary>Pans in base-grid cells regardless of zoom.</summary>
    void PanFine(int horizontal, int vertical);

    /// <summary>Moves one level closer, stopping at individual cells.</summary>
    void ZoomIn(int overviewLevel);

    /// <summary>Moves one level farther out, returning to the overview when it fits.</summary>
    void ZoomOut(int overviewLevel);

    /// <summary>Centers the camera and restores the full-world overview.</summary>
    void Reset();
}
