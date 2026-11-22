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
