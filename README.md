# PengSim

PengSim is a .NET 10 terminal application for exploring a penguin simulation on a **hexagonal map**. The current demo places 5,000 penguins in five clusters.

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
| Arrow keys or WASD | Move the view while zoomed in |
| Shift + arrow keys | Move one cell at a time |
| `R` | Reset view |
| `Q` | Quit |

## Project structure

- `src/PengSim.BL/` — simulation logic
- `src/PengSim.UI/` — terminal visualization and demo
- [How rendering works](src/PengSim.UI/RENDERING.md) — short technical overview

The engine creates an empty simulation. Each simulation owns a grid of penguin IDs and a dictionary of penguins keyed by ID. Zero means an empty cell. `Simulation.AddPenguin` assigns the next ID and updates both structures. The demo supplies the hexagonal footprint and places the colonies.
