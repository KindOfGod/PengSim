namespace PengSim.BL.Contracts;

public interface ISimulation
{
    IEnvironment Environment { get; }
    IReadOnlyList<IEnvironment> Frames { get; }
    IReadOnlyDictionary<int, IPenguin> Penguins { get; }
    int AddPenguin(int column, int row);
    void Step();
}
