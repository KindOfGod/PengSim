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
