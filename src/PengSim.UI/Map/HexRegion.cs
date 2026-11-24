namespace PengSim.UI.Map;

/// <summary>A regular hexagonal region on one level of the lattice.</summary>
internal readonly record struct HexRegion(HexCell Center, int Radius)
{
    /// <summary>Checks whether a cell is on or within the perimeter.</summary>
    public bool Contains(HexCell cell) => cell.DistanceTo(Center) <= Radius;
}
