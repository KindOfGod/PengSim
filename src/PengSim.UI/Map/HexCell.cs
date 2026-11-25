namespace PengSim.UI.Map;

/// <summary>
/// A centered axial drawing coordinate in a base or aggregate hex lattice.
/// (0, 0) is the map center and R grows downward on screen.
/// </summary>
public readonly record struct HexCell(int Q, int R);
