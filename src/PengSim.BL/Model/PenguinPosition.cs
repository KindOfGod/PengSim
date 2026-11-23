namespace PengSim.BL.Model;

/// <summary>A four-byte offset coordinate: X is the column and Y is the row.</summary>
public readonly record struct PenguinPosition(ushort X, ushort Y);
