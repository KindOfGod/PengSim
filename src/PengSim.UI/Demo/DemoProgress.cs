namespace PengSim.UI.Demo;

internal readonly record struct DemoProgress(
    int CompletedSteps,
    int TotalSteps,
    TimeSpan TotalTime,
    TimeSpan IterationTime,
    int Penguins,
    ushort MapSide,
    long MapCells);
