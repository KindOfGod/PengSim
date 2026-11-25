namespace PengSim.UI.Session;

internal readonly record struct RunProgress(
    int CompletedSteps,
    int TotalSteps,
    TimeSpan TotalTime,
    TimeSpan IterationTime,
    ushort Penguins,
    ushort SideA,
    ushort SideB,
    ushort SideC,
    long MapCells);
