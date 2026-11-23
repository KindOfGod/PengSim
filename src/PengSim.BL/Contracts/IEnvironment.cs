namespace PengSim.BL.Contracts;

public interface IEnvironment
{
    const int EmptyCell = 0;
    int Width { get; }
    int Height { get; }
    int this[int column, int row] { get; }
}
