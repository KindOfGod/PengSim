# PengSim

PengSim is a .NET 10 terminal application for exploring a penguin simulation on a **hexagonal map**. Run demo scatters 5,000 penguins across a hexagonal board of radius 150 (301 hexes across the center, 67,951 playable cells) and calculates 1,000 iterations. A progress bar shows the completed iterations and average iterations per second. When generation finishes, review the elapsed time, speed, and simulation details. Press A to analyze the run from its starting positions.

## Run

Install the .NET 10 SDK, then run in an ANSI-capable terminal:

```bash
dotnet run --project src/PengSim.UI
```

If input or output is redirected, the demo prints one plain-text overview and exits.

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
| Home | Pause and jump to the initial frame |
| Left / right arrow | Pause and step back / forward one frame |
| `1`–`9` / `0` | Play at the selected frames per second / pause |
| `R` | Reset view |
| `Q` | Quit |

## Project structure

- `src/PengSim.BL/` — simulation logic
- `src/PengSim.UI/` — terminal visualization and demo
- [How rendering works](src/PengSim.UI/RENDERING.md) — short technical overview

The engine creates an empty simulation. Each simulation owns a grid of penguin IDs and a dictionary of penguins keyed by ID. Zero means an empty cell. `Simulation.AddPenguin` assigns the next ID and updates both structures. The simulation keeps the initial grid and one grid per completed iteration in RAM. For now, each penguin takes one turn per iteration and moves into an empty neighboring hex closer to the center if one is available.
