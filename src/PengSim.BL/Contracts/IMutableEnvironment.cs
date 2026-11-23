namespace PengSim.BL.Contracts;

internal interface IMutableEnvironment : IEnvironment
{
    new int this[int column, int row] { get; set; }
    IMutableEnvironment Copy();
}
