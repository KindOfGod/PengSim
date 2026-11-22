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
