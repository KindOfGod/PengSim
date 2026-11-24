namespace PengSim.BL.Model;

/// <summary>
/// A four-byte axial cell coordinate on the <see cref="Contracts.IHexMap"/>.
/// <see cref="R"/> is the row, counted upward from the bottom side.
/// <see cref="Q"/> is the top-left-to-bottom-right diagonal, counted to the right
/// from the lower-left side. The hexagon's bottom-left corner is (0, 0).
/// </summary>
public readonly record struct PenguinPosition(ushort Q, ushort R);
