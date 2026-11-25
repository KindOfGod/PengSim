namespace PengSim.UI.Demo;

internal readonly record struct DemoProgress(
    int CompletedSteps,
    int TotalSteps,
    TimeSpan TotalTime,
    TimeSpan IterationTime,
    ushort Penguins,
    ushort SideA,
    ushort SideB,
    ushort SideC,
    long MapCells);
