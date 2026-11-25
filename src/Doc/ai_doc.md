# PengSim changes and design decisions

## 2026-11-22 — Initial .NET 10 scaffold

- `PengSim.UI` is the console entry point. It starts and exits without producing output.
- `PengSim.BL` is the class library for the simulation core. Simulation rules and state will be added once their behavior is defined.
- The console app references the class library so it can run the simulation when the core is implemented. The library has no dependency on the console app and can be used from other front ends or tested independently.
- Both projects target `net10.0` and are grouped in `PengSim.slnx` for a single solution build.
- `src/.gitignore` excludes `bin` and `obj` directories under `src`. Project files, source code, the solution, and this log belong in version control.

Add an entry here when making a change or deciding how the simulation is designed. Record what changed and why.

## 2026-11-22 — Simulation model and console demo

- `PengSim.BL.Contracts.ISimulationEngine` exposes simulation creation and the current simulation. The BL Autofac module shares one engine per container. Each successful creation returns a new simulation and makes it current.
- Each `Simulation` owns its grid and a dictionary keyed by penguin ID. IDs start at 1 in each simulation. The grid uses 0 for an empty cell, and `AddPenguin` updates both the grid and the dictionary. A cell can hold one penguin.
- The grid uses zero-based column and row coordinates. Odd columns are shifted down by half a cell to represent flat-topped hexagons.
- The console app builds the Autofac container and injects the engine and Spectre.Console interface into the demo. Demo penguins are placed by the UI, so the engine creates an empty simulation that other callers can populate.

## 2026-11-22 — Staggered grid display

- The demo uses pointy-topped hex coordinates with odd rows offset right by half a cell.
- Autofac injects the UI's `EnvironmentDrawer` into the demo. The drawer reads occupancy from the integer grid and renders hollow hexagons for empty cells and blue filled hexagons for penguins. A rounded Spectre.Console panel frames the board.

## 2026-11-22 — Zoomable console map

- The UI generates a repeatable 240 × 120 world with 5,000 penguins in five clusters. It keeps an occupied-position list for density rendering. The simulation still owns the full grid and penguin dictionary.
- The camera starts with the whole world visible and zooms around its center. The view switches to individual cells once the visible area fits at hex resolution. Both renderers use the world row parity for the horizontal offset.
- The heatmap buckets occupied positions inside the viewport. Fixed count thresholds determine the colors and glyphs so a color has the same meaning across successive views. Detailed rendering only reads visible cells. The simulation supports one penguin per cell.
- Spectre.Console Live refreshes on navigation or terminal resize. With redirected input or output, the app prints a single heatmap and exits.

## 2026-11-22 — Heatmap dots and scale

- The zoomed-out view uses dots colored by density, with a scale below the map showing penguin counts per bucket. The map and scale share one set of color thresholds. The detailed view uses blue filled hexagons for penguins.

## 2026-11-22 — Hierarchical hex heatmap

- Each heatmap level groups seven hexes from the preceding level into one parent. The parent contains a center cell and its six neighbors. The grouping is anchored to world coordinates, so panning preserves parent membership and counts.
- The drawer builds the levels once from the static grid and uses each parent's world bounds to select cells near the viewport. A parent at the edge can include penguins outside the viewport because its count covers the whole parent.
- A shared staggered-row renderer draws actual and aggregated hexes. The panel title shows L0 for actual cells and L1 or higher for parents. The heatmap scale counts penguins per parent hex.

## 2026-11-22 — Hexagonal playground and zoom limit

- The demo's playable footprint is a radius-80 hex centered in the grid. Placement and rendering use that same footprint. The parent lattice is anchored at the center. The full-world heatmap draws a regular hex that encloses all parents at the chosen resolution. Any extra edge cells represent empty padding.
- The first view that fits individual cells is the maximum interactive zoom. Zooming in from that L0 view stops, and zooming out and panning remain available.

## 2026-11-22 — Hexagonal camera and minimap

- The camera stores an axial center and hex radius. Each parent view projects the same hex-shaped selection through the aggregation hierarchy, then renders a regular hex around the projected center. Dim dots mark padding or cells outside the playable region.
- The minimap displays the full world and marks coarse hexes that contain cells from the camera region. On narrow terminals the panels stack. The marker changes at the minimap's displayed cell resolution.
- The heatmap precomputes counts per parent level. The main map and minimap share the renderer, and panning reuses the aggregated counts.

## 2026-11-22 — Compact map panels

- The terminal size determines how much detail fits. Each panel then sizes its border to the rendered hex and title, so the minimap stays one column away from the main panel even on wide terminals.

## 2026-11-22 — Minimap pan steps

- The viewport marker is a regular hex with a fixed radius at each zoom level. Pan keys move its center one minimap cell at a time and place the main camera at that parent cell's center in the base grid. Panning stops before the marker would cross the minimap boundary.

## 2026-11-22 — Clear the interactive terminal on startup

- The console clears once before starting the live map, so the first frame appears at the top. Redirected input or output still produces a single snapshot.

## 2026-11-22 — Global minimap navigation

- The minimap keeps the entire world visible and reserves space for the viewport marker to overlap the world outline. Pan keys can reach edge parents even when the center of an edge parent lies outside the playable area.
- Shift plus an arrow pans one base cell at a time so you can reach locations between minimap centers. On a terminal that can display only one overview cell, regular pan keys also use base-cell steps.

## 2026-11-22 — Precomputed map hierarchy

- Startup builds the full parent hierarchy, including density, base-cell membership, a bounds check, and a playable landing point for every parent. The full-world overview reuses the stored levels directly. Panning and zooming query the stored membership and counts.

## 2026-11-22 — Stable map layout and UI folders

- Both panels stay visible at every zoom level. The main panel uses the largest hex that fits the available rows and columns. Zooming switches between stored aggregation levels while the panel size stays fixed. A magenta ring distinguishes the map outline from grey off-map cells.
- The UI keeps coordinates, the camera, and aggregation in `Map`, terminal drawing in `Rendering`, and sample data in `Demo`. `Program` wires them together at the project root.

## 2026-11-22 — Shared overview resolution

- The minimap has more room, especially on wide terminals. Both panels use the finest full-world aggregation level that fits them. The minimap keeps that level while the main map zooms in.
- When space is tight, the viewport marker uses the room around the overview. The full-world heatmap and the marker's hex shape stay visible at the edges.

## 2026-11-22 — Compact minimap and Information panel

- The minimap border fits the projected bounds of the world and active viewport marker. This keeps the map compact and allows the marker to extend beyond the world at the edges.
- An empty Information panel fills the space below the minimap. The layout reserves at least one content row for it when selecting the shared overview resolution.

## 2026-11-22 — Consistent overview cells

- The main heatmap and minimap show empty cells within the full-world hex as hollow hexagons. The overview displays hollow and filled hexagons inside a purple outline. Grey off-map dots are used only when zoomed in.

## 2026-11-22 — Application frame and simulation details

- The console wraps the maps, Information panel, legend, and controls in a border titled “Penguine Simulation”. Two rows show the simulation name and output path with Spectre.Console's penguin and folder icons.
- The demo is named “Penguin colonies demo” and shows “Unsaved” until an output path is supplied. Rendering accepts an optional path and reserves space for the metadata in its terminal layout.

## 2026-11-22 — Iteration view and colors

- The heatmap, minimap, Information panel, and density legend sit inside a separate iteration frame. The demo displays iteration 0. The simulation metadata and keyboard shortcuts sit outside that frame, leaving room for playback controls when playback is implemented.
- Frame colors distinguish the application, iteration, main map, minimap, and Information panel. Heatmap density colors retain their existing meaning.

## 2026-11-22 — Restrained terminal palette

- Neutral borders group panels. Teal marks the app title, the active iteration frame, and the map outline.
- The density scale progresses from medium blue to pale ice blue as the population rises. Empty cells stay grey. The minimap viewport keeps its diamond glyph and uses a dark neutral background, leaving its density colors readable.

## 2026-11-22 — Warm density scale

- The heatmap and legend use pale yellow for low nonzero density, orange for middle ranges, and red for the highest density. Empty cells are neutral grey. Individual penguins at L0 remain blue.

## 2026-11-22 — Blue-to-red density scale

- The lowest nonzero density shares the single-penguin blue used at L0. The heatmap and legend progress through violet and rose to red at the highest density.

## 2026-11-22 — Finer density bands

- The heatmap has nine occupied density bands plus grey for empty cells. The first four bands distinguish small groups of one to seven penguins. The larger ranges extend to 128 or more so dense clusters remain distinguishable in the overview.
- Counts use the same scale across the heatmap, minimap, and legend. The compact legend gets its endpoints from the density bands.

## 2026-11-22 — Readability refactor

- The console session handles input and redraws. Separate rendering code composes the frames, draws the map panels, fits them to the terminal, and defines the palette.
- The hierarchy builder separates base-cell grouping, parent aggregation, and cached level data. XML summaries document the simulation and UI methods. The console output, navigation, demo data, and simulation behavior are preserved.

## 2026-11-22 — High-contrast minimap selection

- The viewport overlay uses dark diamonds on a teal background to distinguish the selected region from the density map. Its key uses the same styling.

## 2026-11-22 — Terminal rendering and demo placement (PRs #2–6)

- The UI draws directly into a terminal buffer. Each row carries its own ANSI styling so the presenter can update only changed rows. FrameLayout sizes the panels and selects the shared overview level. The README covers controls and the rendering guide explains the coordinate conversions.
- Display buckets use a world-aligned, power-of-two axial lattice. The heatmap scans occupied cells inside the playable hex and stores sparse counts at each coarser level. Both panels reuse those counts during navigation and resize. At L0 the main map reads the selected grid directly.
- The minimap projects the main panel’s rectangular bounds into overview coordinates. Its yellow outline keeps the same dimensions while panning at a fixed zoom, and teal dots mark the world edge.
- Demo placement shuffles playable cells and selects distinct positions so each penguin starts in an empty cell.

## 2026-11-22 — Simulation generation and playback (PR #7)

- The demo places 5,000 penguins on a radius-150 hex. HexCell now lives in the business layer so movement and rendering share axial coordinates and hex distance. Each step clones the previous occupancy grid and moves penguins in placement order. A penguin can enter an empty neighboring cell closer to the center. Squared physical distance breaks ties between inward paths. Placement closes after the first step.
- Generating 1,000 steps retains 1,001 grids in memory, including the starting positions. The interactive menu opens the demo with 2, shows generation progress and timing, and waits for A to analyze the run. Redirected input or output produces one plain-text overview of the initial frame.
- Analysis starts paused at frame zero. Left and right step through history, Home rewinds and pauses, 1–9 select frames per second, and 0 pauses. Playback stops at the final frame. Selecting a different iteration rebuilds the heatmap for its occupancy grid.

## 2026-11-23 — Interface-based simulation and UI

- ISimulationEngine returns ISimulation. Frames expose the read-only IEnvironment grid and the ID lookup exposes IPenguin. Grid writes and frame copying use the internal IMutableEnvironment contract so model code can advance the simulation while renderers can only read frames.
- The console entry point registers the demo, map, and rendering services with Autofac and resolves ISimulationDemo. Interface-returning factories create cameras and heatmaps for a footprint, buffers for a terminal size, and presenters for an interactive session. Coordinates, panel bounds, and pixels are value types.

## 2026-11-23 — Render timing

Run `PENGSIM_PROFILE=1 dotnet run --project src/PengSim.UI` to profile the analysis view. Each displayed map frame contributes one preparation sample (composing and encoding rows) and one output sample (presenting those rows). The initial heatmap build has its own timer. The report is written when the view exits, to `pengsim-profile.log` or the file named by `PENGSIM_PROFILE_PATH`. Simulation generation and later heatmap rebuilds are outside the frame timers.

## 2026-11-23 — Isolate the demo movement rule

IPenguin.Move delegates to the private MoveTowardCenterForDemo method. This identifies the inward step as a temporary demo rule and keeps it replaceable through the existing movement contract. The root AGENTS.md records the interface, member ordering, and documentation conventions.

## 2026-11-23 — One environment contract for playback

IEnvironment exposes dimensions and read-only occupancy. Simulation owns the row-major cell array. During placement the initial frame reflects added penguins. Each step clones the array once, applies the demo moves to that copy in placement order, and appends a read-only view of the completed grid. Earlier frames keep their own arrays. This removes the separate writable environment interface.

## Simulation access through the engine

ISimulationEngine is the public entry point for creation, placement, stepping, and playback history. The demo requests each step through the engine and passes its read-only Iterations list to the viewer. Index zero reflects placement until the first step. Each subsequent index holds a completed grid. Creating a new run starts a separate history. The mutable simulation is internal to the business layer.

## Demo generation and screen responsibilities

SimulationDemo coordinates the start menu, generation, and analysis. It places penguins and advances the engine, passing timing and population snapshots to IDemoScreen. DemoScreen owns menu shortcuts, progress formatting, and resize redraws. Redirected runs skip the menus and render the initial overview.

## Clear the terminal on quit

Quitting an interactive menu or playback clears the visible display and returns the cursor to the top left for the shell prompt. TerminalPresenter performs this cleanup when its session ends.

## Playback rewind shortcut

Backspace replaces Home for pausing playback and returning to iteration zero. The camera keeps its current zoom and position.

## Playback shortcut colors

The playback footer uses the iteration border’s accent color for each key and the terminal’s default foreground for its action, making the shortcuts easier to scan.

## Restore population values in the heatmap legend

Each swatch shows the inclusive penguin count range for one displayed hex. Map colors and legend labels use the same thresholds. Narrow layouts reserve extra rows so the high-density ranges stay visible alongside the low-density ranges.

## Interface-only contracts

Contracts contains only interfaces. The Environment and Penguin implementations live in Model alongside Simulation, which constructs them. Consumers continue to access frames and penguins through IEnvironment and IPenguin.

## Position arrays and 16-bit simulation limits

The engine supports 1–65,000 columns, 1–65,000 rows and up to 65,000 penguins. IDs start at 1 and use ushort. X and Y are zero-based ushort offset coordinates. Bounds checks precede conversion from signed hex geometry so a move beyond the board cannot wrap to the opposite edge.

Each completed iteration owns one PenguinPosition array indexed by ID minus one. Its payload is four bytes per penguin. Frame zero uses a growable array during placement and closes when stepping begins. Movement updates one occupied-position set in placement order so later penguins see earlier moves. The viewer rebuilds its base-cell and aggregate indexes from the selected frame. Neither the simulation nor its history allocates a full board grid.

## Board dimensions use ushort

The engine accepts ushort width and height, and each read-only iteration reports both dimensions as ushort. Board cell lookups take ushort column and row. The engine enforces 1–65,000 cells per axis. Camera and axial arithmetic use signed intermediates to represent cells on either side of the origin.

## Maximum-size demo

The demo exercises both engine limits together: 65,000 penguins and a 65,000 × 65,000 grid for 1,000 steps. The playable hex has radius 32,499 and is centered at offset (32,500, 32,500), the same center used by movement. Placement samples random board coordinates and retries cells outside the hex or already occupied, keeping setup memory proportional to population.

## Extended demo and temporary fast playback

The maximum-size demo now runs for 100,000 steps. The initial placement and 100,000 completed snapshots contain about 26 GB of coordinates. During analysis, M temporarily selects 240 iterations per second. Playback follows elapsed time and displays the latest due iteration when drawing takes longer than a frame interval. Manual stepping still visits individual frames.

## Demo population for the 100,000-step run

The demo places 10,000 penguins on the 65,000 × 65,000 board. Its 100,001 frames hold 4,000,040,000 bytes of position data. The engine still supports up to 65,000 penguins.

## Smaller demo population

The demo now places 1,000 penguins. At four bytes per position, the initial frame and 100,000 completed steps hold 400,004,000 bytes of coordinate data.

## 250 × 250 demo and playback controls

The demo places 500 penguins on a 250 × 250 grid and records 1,000 steps. The 1,001 frames contain 2,002,000 bytes of coordinates. The temporary M shortcut has been removed. Keys 1–9 select playback speed and 0 pauses.

## Parameter-based demo setup

The demo now builds an `ISimulationParameters` instance and passes it to `CreateSimulation`, so the engine handles placement instead of the UI. The parameters use three equal sides of 125 cells because the renderer and heatmap only draw regular hexes. `GridDimensions` gives a 249 × 249 board. The UI's `HexFootprint` centers the same radius-124 hex on that board, so both layers use the same cells. If the sides differ, the UI throws at startup instead of drawing the wrong shape.

## Start menu with three simulation sources

The main menu now offers [1] Simulation from parameters, [2] Simulation from code, and [3] Demo simulation. All three produce an `ISimulationParameters` and use the same steps after that: validate, build the footprint, generate, show the summary, and analyze. The step count now comes from `SimulatedSteps` instead of a demo constant.

- `SimulationParameterForm` asks for one field per prompt, pre-filled with the demo values, and repeats a prompt until the input parses. It does not ask for the placement seed: the UI sets a random seed when it builds the parameters, because users want new layouts rather than to choose a seed. The code shown in the analysis view keeps the seed, so a run can still be reproduced. It does not ask for the distribution because only Uniform exists.
- The form now asks for all three side lengths (eight fields in total), and every prompt shows its allowed range. A prompt repeats until the value is within that range, so mistakes are caught at the field instead of after the whole form. Side C allows at most 65537 − max(A, B), which keeps A + C − 2 and B + C − 2 within `ushort`. The population allows at most the map's cell count, capped at 65535. Prefilled defaults are clamped to these limits. Wind speed must be 0 or more, and all decimals must be finite. Unequal sides pass the form but are still rejected by `HexFootprint`, because the renderer only draws regular hexes.
- The code option decodes through `ISimulationParametersCodec`, which is now registered in the UI container. An invalid code keeps the prompt open and shows an error.
- `Validate()` failures are shown on an error screen, followed by a return to the menu. So are boards wider than `MaxDimension`, which would otherwise wrap when converted to `ushort`, and hexes with unequal sides.
- `DemoScreen` gained a line-input prompt and an error screen. Its key loop is shared, so every screen redraws on resize. Escape in any prompt returns to the menu.
- Redirected runs skip the menu and render the demo overview as before.

## Parameter code in the analysis view

The analysis header now shows the run's parameter code next to its name: `Name: … Code: …`. Pasting it into Simulation from code recreates the same run. This works because placement is seeded and the codec stores floats by their raw bits. A temporary engine test confirmed that a decoded code gives the same frame zero and the same later frames. The header row was chosen because it has almost the full terminal width, while codes are 30–60 characters long and would be clipped in the Information panel. `IEnvironmentDrawer.Show` and `IFrameRenderer.RenderRows` take the code as an extra string argument. The UI encodes the code with `ISimulationParametersCodec` before opening the view.

The README now documents the parameter model, the `GridDimensions` formula, the code format, engine-side seeded placement, the 65,535 limits, and `null` for empty cells. Before, it still described `AddPenguin`, a 65,000 limit, and zero as the empty-cell value.

## Hex coordinates throughout the BL

The BL no longer uses offset or Cartesian coordinates. Before, positions were offset column/row pairs on the rectangle around the hexagon. Each penguin move converted them to axial and back, and the inside check, `GridDimensions`, and row-parity rules all existed only because of that rectangle.

- `PenguinPosition` is now `(ushort Q, ushort R)` and still takes 4 bytes. The origin is the hexagon's bottom-left corner. R counts rows upward, and Q counts the top-left-to-bottom-right diagonals to the right. This origin keeps every coordinate non-negative, so storage stays `ushort` without sign handling. Neighbors are (±1, 0), (0, ±1), and ±(1, 1).
- The new `IHexMap` / `HexMap` owns all geometry:
  - side lengths, `MaxQ` (A + C − 2) and `MaxR` (B + C − 2)
  - `CellCount`, which uses `long` because the old `int` formula overflowed for large sides
  - `Center`: the hexagon is point-symmetric, so its center is (MaxQ / 2, MaxR / 2), rounded down
  - `Contains`: three range checks, one for each pair of opposite sides
  - `Distance`: max(|Δq|, |Δr|, |Δq − Δr|)
  - `SquaredEuclideanDistance`: Δq² − ΔqΔr + Δr², which the movement rule uses to break ties
  - `GetNeighbors` and seeded `GetRandomPosition`
- `HexMapTests` check all of this against brute force for every side combination from 1 to 6. That covers the cell count, side lengths, corners, the center, and breadth-first distances.
- `IEnvironment` exposes `Map` instead of `Width`/`Height`. Its indexer takes a `PenguinPosition`. `IPenguin.Move` receives the map instead of a width and height. `ISimulationParameters.GridDimensions` has been removed.
- `SimulationParameters.Validate` builds a `HexMap`, which checks the side lengths and the new coordinate limit. `ISimulationEngine.MaxDimension` became `MaxCoordinate`.
- `Simulation`'s two constructors now share one private constructor, so the setup is no longer duplicated.
- `HexCell` moved to the UI (`PengSim.UI.Map`) because only drawing code still uses it. `HexFootprint` is built from an `IHexMap` and converts map positions to centered drawing coordinates. This conversion only shifts the origin and flips R. The camera's square screen grid stays in the UI as a display concern. The UI shows the camera center in map coordinates (q, r). The summary shows the map side and cell count instead of a grid size.

## Fast run or progress bar

After a simulation is chosen and validated, the UI asks how to calculate it: Enter runs all steps at once with `ISimulationEngine.Exec()`, and P runs them one at a time with `Step()` and the progress bar, as before. Escape goes back to the menu. The fast mode only shows a static "Calculating all steps…" panel, so it skips the progress redraws and the per-step stopwatch reads. Redirected runs always use the fast mode. The summary screen after the run shows the same timings in both modes, so the two can be compared.

## BL documentation and readability pass

Inline comments in the BL were replaced with descriptive names (for example `isBetweenSidesA`, `stepsToCenter`, `previousPositions`) or moved into `<remarks>`. Every type, member, and field now has a doc comment, and implementations use `<inheritdoc />`. Several doc comments were wrong and have been corrected:

- The codec's remarks described a fixed-point float format with `FixedPointScale` and `SignedOffset`, and an optional seed. None of these exist: floats are stored as their raw IEEE 754 bits. The remarks now describe the real format.
- `CreateSimulation(string)` claimed to return "the created simulation object". It returns the engine.
- `IPenguin.Id` mentioned an occupancy grid that no longer exists. It now says the ID is the 1-based index into each step's positions.
- `SimulationParameters` said it was "typically read from JSON". It is entered in the UI or decoded from a code. An unused `System.Text.Json` using was removed.

Small code changes made along the way:

- The codec no longer maps the wind direction to (-180, 180] before encoding. That only mattered for the old fixed-point range, and with raw bits it could add rounding errors. Old codes still decode the same way.
- `Decode` now requires exactly ten segments instead of at least ten, so codes with trailing extra segments are rejected.
- `Simulation` dropped the unused `SimulationCode` property and the private constructor that existed only for it. `Parameters` is now a private field.
- `SimulationEngine.Step` and `Exec` share a guard helper that uses braces, as the repository conventions require.

## Clearer code errors, printed code, and parameter tests

- The code prompt now shows the exception message from `Decode` instead of a generic text, such as the expected segment count or which value is out of range. `DemoScreen` wraps long errors over several lines so they are not cut off at the panel edge.
- After the app exits, the code of the generated run is printed to the normal terminal, so it can be copied after the alternate screen closes. The analysis header still shows it as well. Nothing is printed when the user quits from the menu without generating a run.
- `ISimulationEngine.CreateSimulation(string code)` stays, even though the UI decodes codes itself. Future modules will connect to the engine and start runs from codes.
- New tests cover the codec (wrong segment count, empty code, non-hex segments, values that overflow a `ushort`, NaN floats, a side of 0, an unknown distribution, normalizing a negative wind direction, and round trips of the demo and boundary parameters) and `SimulationParameters.Validate` (each rule, plus a population that fills every cell).
- The README now describes the three-side form, the range hints, the Enter/P run choice, and the printed code.

## The UI creates simulations through the engine

The code menu entry now passes the pasted code straight to `ISimulationEngine.CreateSimulation(string)` instead of decoding it in the UI. This uses the same entry point that future modules will use. The parameter form and the demo still call `CreateSimulation(ISimulationParameters)`.

- `ISimulationEngine` gained `Parameters`, the validated parameters of the active run, or null before creation. The UI reads the step count and the code it displays from there, so the shown code always describes the run that was calculated.
- The simulation is now created right after the menu choice, before the Enter/P run-mode prompt. Invalid parameters or codes are caught at that point. The footprint is built from `engine.Iterations[0].Map`, and unequal sides are reported on a "Cannot draw this map" screen.
- Creation time is still counted in the "Elapsed" total, but is now measured separately and added to the step time.

## Hexes with unequal sides

The renderer now draws any hex with sides A, B, and C, so the form's three side lengths and codes with unequal sides run instead of ending on an error screen.

- `HexRegion` changed from a center and radius to a range for each of Q, R, and S = −Q − R. Every hexagon whose opposite sides are equal is exactly such a set, and a regular hex of radius n has all three ranges at [−n, n]. The region provides containment, a one-cell expansion, the cells of a row, and the screen column range.
- `HexFootprint` builds the region straight from the `IHexMap` bounds: Q in [0, MaxQ], R in [0, MaxR], and the map's R − Q in [−(A − 1), B − 1], shifted by the center and with R flipped. The equal-sides check and the square storage grid were removed. `ClampToBounds` keeps the camera inside the screen rectangle around the region.
- `HexHeatmap` measures each zoom level's region by converting the perimeter cells (both ends of every row, and the full top and bottom rows) with `ToLevel` and taking the ranges. The old radius only scanned row ends; the full top and bottom rows were added because the cube-rounding correction can shift R inside a row. `FitWorld` now takes a column and row budget instead of a radius. Each level caches its column width, so the per-frame overview fit does not loop over rows.
- The aggregation lattice stays anchored at the map center. That hexagon is point-symmetric even with unequal sides, so opposite sides get the same padding at coarse levels. A corner anchor (or 7-cell clusters) was considered: neither makes a side exact, because hex buckets have zigzag edges, and 7-cell clusters would also rotate the lattice by about 19° per level.
- The world border is now "inside the region expanded by one cell, but outside the region". For regular hexes this is the same as the old "distance == R + 1".
- The camera's staggered offset conversion moved into `MapCamera` and no longer depends on the hex radius. `HexCell.DistanceTo` became unused and was removed.
- The summary screen shows all three sides. The "Cannot draw this map" screen was removed.
- Regression check: the redirected overview of the 125/125/125 demo is identical to the output from before the change, once penguin glyphs and the code are normalized.

## Wind direction keeps in-range angles exactly

`SimulationParameters.WindDirectionDegrees` used `(value % 360 + 360) % 360`. The `+ 360` ran for every angle and rounded at the float spacing near 360 (about 0.00003°), so 0.00001 became 0 and 1.1 became 1.1000061. The code then stored the changed value.

- The setter now adds 360 only when the remainder is negative. Float `%` is exact, so angles in [0, 360) are kept bit for bit.
- A tiny negative angle such as -0.00001 still rounds up to exactly 360 when 360 is added. It is mapped to 0, so the stored angle stays in [0, 360).
- Tests check that in-range angles keep their exact bits, that 725 and -0.00001 normalize to 5 and 0, and that 0.00001 survives a codec round trip.

## Shared run workflow moved out of Demo

Parameter runs, replay codes, and the built-in demo all go through the same menu, run-mode prompt, progress screen, and analysis view, but the workflow lived in `Demo/` with demo-prefixed names.

- The workflow moved to `Session/` (`PengSim.UI.Session`): `MenuChoice`, `RunMode`, the parameter form, the screen, the progress record, and the orchestration.
- Types that serve every run were renamed: `SimulationDemo` → `SimulationSession`, `DemoScreen` → `SessionScreen`, `DemoProgress` → `RunProgress`, with their interfaces.
- The demo parameters and name moved from the session into the static `Demo/DemoPreset`, the only demo-specific piece. `MenuChoice.Demo` and the menu text keep the word, because that entry is the demo.
- Behavior is unchanged.
