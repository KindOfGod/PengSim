using System.Diagnostics;
using PengSim.BL.Contracts;
using PengSim.BL.Model;
using PengSim.BL.Parameters;
using PengSim.UI.Map;
using PengSim.UI.Rendering;

namespace PengSim.UI.Demo;

/// <summary>
/// Lets the user pick a simulation from the main menu, generates it, and opens the analysis view.
/// </summary>
internal sealed class SimulationDemo(
    ISimulationEngine engine,
    IEnvironmentDrawer drawer,
    ISimulationParameterForm parameterForm,
    ISimulationParametersCodec codec,
    Func<IDemoScreen> createScreen) : ISimulationDemo
{
    // The renderer draws regular hexes, so the demo uses equal sides.
    private const ushort HexSideLength = 125;
    private const ushort PenguinCount = 500;
    private const ushort IterationCount = 1_000;
    private const float DemoTemperatureCelsius = -20f;
    private const string DemoName = "Penguin demo";
    private const string ParametersName = "Custom simulation";
    private const string CodeName = "Simulation from code";
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(100);

    private static readonly ISimulationParameters DemoParameters = new SimulationParameters
    {
        SideA = HexSideLength,
        SideB = HexSideLength,
        SideC = HexSideLength,
        PenguinPopulation = PenguinCount,
        PenguinDistribution = PenguinDistribution.Uniform,
        PenguinDistributionSeed = (ushort)Random.Shared.Next(ushort.MaxValue + 1),
        SimulatedSteps = IterationCount,
        WindDirectionDegrees = 0f,
        WindSpeedMetersPerSecond = 0f,
        TemperatureCelsius = DemoTemperatureCelsius,
    };

    public void Run()
    {
        var interactive = !Console.IsInputRedirected && !Console.IsOutputRedirected;
        if (!interactive)
        {
            var board = CreateFootprint(DemoParameters);
            Generate(DemoParameters, RunMode.Fast, screen: null);
            drawer.Show(engine.Iterations, board, DemoName, codec.Encode(DemoParameters));
            return;
        }

        using var screen = createScreen();
        while (screen.WaitForMenuChoice() is { } choice)
        {
            if (ChooseParameters(choice, screen) is not { } selection)
            {
                continue;
            }

            var (parameters, name) = selection;
            if (TryCreateFootprint(parameters, screen) is not { } board)
            {
                continue;
            }

            if (screen.WaitForRunMode(name) is not { } mode)
            {
                continue;
            }

            var result = Generate(parameters, mode, screen);
            if (screen.WaitForAnalysis(result))
            {
                drawer.Show(engine.Iterations, board, name, codec.Encode(parameters));
            }
            return;
        }
    }

    /// <summary>Returns the parameters and display name for a menu entry, or null to go back to the menu.</summary>
    private (ISimulationParameters Parameters, string Name)? ChooseParameters(MenuChoice choice, IDemoScreen screen)
    {
        switch (choice)
        {
            case MenuChoice.FromParameters:
                return parameterForm.Read(screen, DemoParameters) is { } parameters
                    ? (parameters, ParametersName)
                    : null;
            case MenuChoice.FromCode:
                return ReadCode(screen) is { } decoded
                    ? (decoded, CodeName)
                    : null;
            case MenuChoice.Demo:
                return (DemoParameters, DemoName);
            default:
                throw new ArgumentOutOfRangeException(nameof(choice), choice, "Unknown menu entry.");
        }
    }

    /// <summary>Asks for a code until it decodes into valid parameters, or returns null on Escape.</summary>
    private ISimulationParameters? ReadCode(IDemoScreen screen)
    {
        var text = "";
        string? error = null;
        while (true)
        {
            var input = screen.ReadLine(CodeName, "Paste the simulation code", text, error);
            if (input is null)
            {
                return null;
            }

            try
            {
                return codec.Decode(input.Trim());
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
            {
                text = input;
                error = "Invalid code or invalid parameters.";
            }
        }
    }

    /// <summary>Validates the parameters for the engine and renderer, or shows why they cannot run.</summary>
    private static HexFootprint? TryCreateFootprint(ISimulationParameters parameters, IDemoScreen screen)
    {
        try
        {
            parameters.Validate();
            return CreateFootprint(parameters);
        }
        catch (ArgumentException exception)
        {
            screen.ShowError("Invalid parameters", exception.Message);
            return null;
        }
    }

    /// <summary>Builds the drawing footprint; the map checks the side lengths and the renderer requires equal sides.</summary>
    private static HexFootprint CreateFootprint(ISimulationParameters parameters)
    {
        return new HexFootprint(CreateMap(parameters));
    }

    private static IHexMap CreateMap(ISimulationParameters parameters)
    {
        return new HexMap(parameters.SideA, parameters.SideB, parameters.SideC);
    }

    /// <summary>
    /// Creates and calculates the simulation. <see cref="RunMode.Fast"/> runs all steps at once with
    /// <see cref="ISimulationEngine.Exec"/>; <see cref="RunMode.WithProgress"/> runs one step at a time
    /// and shows progress. Without a <paramref name="screen"/>, nothing is drawn.
    /// </summary>
    private DemoProgress Generate(ISimulationParameters parameters, RunMode mode, IDemoScreen? screen)
    {
        var totalTime = Stopwatch.StartNew();
        engine.CreateSimulation(parameters);

        var iterationTime = Stopwatch.StartNew();
        if (mode == RunMode.WithProgress && screen is not null)
        {
            StepWithProgress(parameters.SimulatedSteps, screen, totalTime, iterationTime);
        }
        else
        {
            screen?.ShowRunning();
            engine.Exec();
        }

        iterationTime.Stop();
        totalTime.Stop();
        return GetProgress(parameters.SimulatedSteps, totalTime.Elapsed, iterationTime.Elapsed);
    }

    /// <summary>Runs every step separately and redraws the progress at most once per <see cref="ProgressInterval"/>.</summary>
    private void StepWithProgress(int totalSteps, IDemoScreen screen, Stopwatch totalTime, Stopwatch iterationTime)
    {
        var nextUpdate = TimeSpan.Zero;
        for (var step = 1; step <= totalSteps; step++)
        {
            engine.Step();
            var elapsed = iterationTime.Elapsed;
            if (elapsed >= nextUpdate || step == totalSteps)
            {
                screen.ShowProgress(GetProgress(totalSteps, totalTime.Elapsed, elapsed));
                nextUpdate = elapsed + ProgressInterval;
            }
        }
    }

    private DemoProgress GetProgress(int totalSteps, TimeSpan totalTime, TimeSpan iterationTime)
    {
        var map = engine.Iterations[0].Map;
        return new DemoProgress(engine.Iterations.Count - 1, totalSteps, totalTime,
            iterationTime, engine.Penguins.Count, map.SideA, map.CellCount);
    }
}
