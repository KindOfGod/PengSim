# PengSim

PengSim is a .NET 10 terminal application for exploring a penguin simulation on a **hexagonal map**. The main menu offers three ways to start a run. **[1] Simulation from parameters** asks for each value in turn, pre-filled with the demo values. The placement seed is not asked for; the UI picks it at random, and the code in the analysis header keeps it. **[2] Simulation from code** decodes a parameter code produced by `SimulationParametersCodec`. **[3] Demo simulation** places 500 penguins on a hex with 125-cell sides (46,501 cells) and calculates 1,000 iterations, retaining 1,001 frames (about 2 MB of coordinate data, plus runtime overhead). Escape in a prompt returns to the menu. The renderer only draws hexes with three equal sides, so the parameter form asks for one side length and codes with unequal sides are rejected. The analysis header shows the run's parameter code next to its name. Paste it into **[2]** to recreate the same run, including the penguin placement, which is seeded. A progress bar shows the completed iterations and average iterations per second. When generation finishes, review the elapsed time, speed, and simulation details. Press A to analyze the run from its starting positions.

## Run

Install the .NET 10 SDK, then run in an ANSI-capable terminal:

```bash
dotnet run --project src/PengSim.UI
```

If input or output is redirected, the menu is skipped and the demo prints one plain-text overview and exits.

## Measure render times

```bash
PENGSIM_PROFILE=1 dotnet run --project src/PengSim.UI
```

Start a simulation from the menu, press **A** to analyze it, then navigate or play back frames. Press **Q** to write `pengsim-profile.log` in the working directory. The report includes the rendered frame count, initial heatmap build time, average frame preparation time, and average and slowest output updates. Set `PENGSIM_PROFILE_PATH` to choose another file. Redirected runs profile one overview frame.

## Visualization

The **main map** shows the area you are looking at. The **minimap** shows the whole world, with yellow hexes outlining your current view.

- **Zoom out (`-`):** Nearby hexes are grouped into larger hexagonal areas. Their color shows how many penguins are in each area.
- **Zoom in (`+`):** Each area becomes smaller, revealing more detail. At **L0**, each hex represents one actual map cell.
- **Colors:** Blue indicates fewer penguins; purple and red indicate higher density. Hollow hexes are empty.
- **Map edges:** Teal dots mark the boundary of the hexagonal world. The viewing window is rectangular, so it can also show empty space outside the world.

| Key | Action |
| --- | --- |
| `+` / `-` | Zoom in / out |
| WASD | Move the view while zoomed in |
| Shift + WASD | Move one cell at a time |
| Backspace | Pause and jump to the initial frame |
| Left / right arrow | Pause and step back / forward one frame |
| `1`–`9` / `0` | Play at the selected frames per second / pause |
| `R` | Reset view |
| `Q` | Quit |

## Project structure

- `src/PengSim.BL/` — simulation logic
- `src/PengSim.UI/` — terminal visualization and demo
- [AI design log](src/Doc/ai_doc.md) — change history and design decisions
- [How rendering works](src/PengSim.UI/RENDERING.md) — short technical overview

## Simulation storage and limits

A run is described by `ISimulationParameters`: three hexagon side lengths A, B and C, the penguin population, distribution and seed, the number of steps, wind and temperature. `IHexMap` holds the map built from the sides and does all hex geometry: the inside check, neighbors, distances, the center cell, and random cells. `ISimulationParametersCodec` turns parameters into a short code and back. The code is hex numbers separated by `y`, with floats stored by their raw bits, so decoding gives back exactly the same values.

Positions use axial hex coordinates with the origin (0, 0) at the hexagon's bottom-left corner. **R** is the row, counted upward from the bottom side. **Q** is the top-left-to-bottom-right diagonal, counted to the right from the lower-left side. Neighbors differ by (±1, 0), (0, ±1) or ±(1, 1). A cell is on the map when 0 ≤ R ≤ B + C − 2, 0 ≤ Q ≤ A + C − 2, and −(A − 1) ≤ R − Q ≤ B − 1, so coordinates are never negative.

A run supports up to **65,535 penguins** (`ISimulationEngine.MaxPenguins`). The largest coordinate, A + C − 2 for Q and B + C − 2 for R, may be at most **65,535** (`MaxCoordinate`). Penguin IDs are consecutive `ushort` values starting at 1. Occupancy queries (`environment[position]`) return `null` for an empty cell.

Each iteration stores an array of `PenguinPosition` values in ID order. To read a penguin’s position, use `engine.Iterations[iteration].Positions[id - 1]`. Each entry contains two 16-bit coordinates (4 bytes). For 1,000 penguins and 1,000 steps, the coordinate payload of the 1,001 snapshots is 4,004,000 bytes, plus collection overhead. Board area does not affect this payload.

When a simulation is created, the engine places the population itself. It draws random cells from `new Random(seed)`, keeps the ones inside the hexagon that are still empty, and repeats until everyone is placed. The same parameters therefore always give the same frame zero. Each step writes a fresh position array, so earlier frames remain available for read-only playback. A single occupied-position set serves collision checks while the demo moves penguins toward the center in ID order.
