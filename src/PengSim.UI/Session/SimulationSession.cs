using System.Diagnostics;
using PengSim.BL.Contracts;
using PengSim.UI.Demo;
using PengSim.UI.Map;
using PengSim.UI.Rendering;

namespace PengSim.UI.Session;

/// <summary>
/// Lets the user pick a simulation from the main menu, generates it, and opens the analysis view.
/// </summary>
internal sealed class SimulationSession(
    ISimulationEngine engine,
    IEnvironmentDrawer drawer,
    ISimulationParameterForm parameterForm,
    ISimulationParametersCodec codec,
    Func<ISessionScreen> createScreen) : ISimulationSession
{
    private const string ParametersName = "Custom simulation";
    private const string CodeName = "Simulation from code";
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>Gets the parameters of the simulation the engine is running, which exists after a successful creation.</summary>
    private ISimulationParameters ActiveParameters =>
        engine.Parameters ?? throw new InvalidOperationException("No simulation was created yet.");

    public void Run()
    {
        var interactive = !Console.IsInputRedirected && !Console.IsOutputRedirected;
        if (!interactive)
        {
            var creationTime = Measure(() => engine.CreateSimulation(DemoPreset.Parameters));
            var board = new HexFootprint(engine.Iterations[0].Map);
            Generate(RunMode.Fast, screen: null, creationTime);
            drawer.Show(engine.Iterations, board, DemoPreset.Name, codec.Encode(ActiveParameters));
            return;
        }

        string? simulationCode;
        using (var screen = createScreen())
        {
            simulationCode = RunMenu(screen);
        }

        if (simulationCode is not null)
        {
            Console.WriteLine($"Simulation code: {simulationCode}");
        }
    }

    /// <summary>
    /// Shows the menu until a simulation is generated, then offers the analysis view.
    /// Returns the code of the generated simulation, or null when the user quits from the menu.
    /// </summary>
    private string? RunMenu(ISessionScreen screen)
    {
        while (screen.WaitForMenuChoice() is { } choice)
        {
            if (CreateChosenSimulation(choice, screen) is not { } created)
            {
                continue;
            }

            var (name, creationTime) = created;
            if (screen.WaitForRunMode(name) is not { } mode)
            {
                continue;
            }

            var board = new HexFootprint(engine.Iterations[0].Map);
            var result = Generate(mode, screen, creationTime);
            var simulationCode = codec.Encode(ActiveParameters);
            if (screen.WaitForAnalysis(result))
            {
                drawer.Show(engine.Iterations, board, name, simulationCode);
            }
            return simulationCode;
        }

        return null;
    }

    /// <summary>
    /// Creates the simulation for a menu entry in the engine. Returns its display name and how long
    /// the creation took, or null to go back to the menu.
    /// </summary>
    private (string Name, TimeSpan CreationTime)? CreateChosenSimulation(MenuChoice choice, ISessionScreen screen)
    {
        switch (choice)
        {
            case MenuChoice.FromParameters:
                return parameterForm.Read(screen) is { } parameters
                    && TryCreateFromParameters(parameters, screen) is { } parametersTime
                    ? (ParametersName, parametersTime)
                    : null;
            case MenuChoice.FromCode:
                return CreateFromCode(screen) is { } codeTime
                    ? (CodeName, codeTime)
                    : null;
            case MenuChoice.Demo:
                return (DemoPreset.Name, Measure(() => engine.CreateSimulation(DemoPreset.Parameters)));
            default:
                throw new ArgumentOutOfRangeException(nameof(choice), choice, "Unknown menu entry.");
        }
    }

    /// <summary>Creates the simulation from the parameters, or shows why they are invalid and returns null.</summary>
    private TimeSpan? TryCreateFromParameters(ISimulationParameters parameters, ISessionScreen screen)
    {
        try
        {
            return Measure(() => engine.CreateSimulation(parameters));
        }
        catch (ArgumentException exception)
        {
            screen.ShowError("Invalid parameters", exception.Message);
            return null;
        }
    }

    /// <summary>
    /// Asks for a code until the engine creates a simulation from it, and returns how long that took.
    /// Returns null on Escape.
    /// </summary>
    private TimeSpan? CreateFromCode(ISessionScreen screen)
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
                var code = input.Trim();
                return Measure(() => engine.CreateSimulation(code));
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
            {
                text = input;
                error = exception.Message;
            }
        }
    }

    /// <summary>
    /// Calculates the created simulation. <see cref="RunMode.Fast"/> runs all steps at once with
    /// <see cref="ISimulationEngine.Exec"/>; <see cref="RunMode.WithProgress"/> runs one step at a time
    /// and shows progress. Without a <paramref name="screen"/>, nothing is drawn.
    /// <paramref name="creationTime"/> is added to the reported total time.
    /// </summary>
    private RunProgress Generate(RunMode mode, ISessionScreen? screen, TimeSpan creationTime)
    {
        var totalSteps = ActiveParameters.SimulatedSteps;
        var iterationTime = Stopwatch.StartNew();
        if (mode == RunMode.WithProgress && screen is not null)
        {
            StepWithProgress(totalSteps, screen, creationTime, iterationTime);
        }
        else
        {
            screen?.ShowRunning();
            engine.Exec();
        }

        iterationTime.Stop();
        return GetProgress(totalSteps, creationTime + iterationTime.Elapsed, iterationTime.Elapsed);
    }

    /// <summary>Runs every step separately and redraws the progress at most once per <see cref="ProgressInterval"/>.</summary>
    private void StepWithProgress(int totalSteps, ISessionScreen screen, TimeSpan creationTime, Stopwatch iterationTime)
    {
        var nextUpdate = TimeSpan.Zero;
        for (var step = 1; step <= totalSteps; step++)
        {
            engine.Step();
            var elapsed = iterationTime.Elapsed;
            if (elapsed >= nextUpdate || step == totalSteps)
            {
                screen.ShowProgress(GetProgress(totalSteps, creationTime + elapsed, elapsed));
                nextUpdate = elapsed + ProgressInterval;
            }
        }
    }

    /// <summary>Collects the progress of the active run for the progress and summary screens.</summary>
    private RunProgress GetProgress(int totalSteps, TimeSpan totalTime, TimeSpan iterationTime)
    {
        var map = engine.Iterations[0].Map;
        return new RunProgress(engine.Iterations.Count - 1, totalSteps, totalTime,
            iterationTime, (ushort)engine.Penguins.Count, map.SideA, map.SideB, map.SideC, map.CellCount);
    }

    /// <summary>Runs the action and returns how long it took.</summary>
    private static TimeSpan Measure(Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        action();
        return stopwatch.Elapsed;
    }
}
