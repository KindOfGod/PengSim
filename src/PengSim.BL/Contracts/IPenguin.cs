namespace PengSim.BL.Contracts;

public interface IPenguin
{
    int Id { get; }
    internal void Move(int[,] cells);
}
