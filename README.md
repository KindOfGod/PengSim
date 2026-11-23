# PengSim

PengSim is a .NET 10 terminal application for exploring a penguin simulation on a **hexagonal map**. Run demo places 1,000 penguins on distinct random cells inside a radius-32,499 hex on a 65,000 × 65,000 grid. It calculates 100,000 iterations and retains 100,001 frames (about 400 MB of coordinate data, plus runtime overhead). A progress bar shows the completed iterations and average iterations per second. When generation finishes, review the elapsed time, speed, and simulation details. Press A to analyze the run from its starting positions.

## Run

Install the .NET 10 SDK, then run in an ANSI-capable terminal:

```bash
dotnet run --project src/PengSim.UI
```

If input or output is redirected, the demo prints one plain-text overview and exits.

## Measure render times

```bash
PENGSIM_PROFILE=1 dotnet run --project src/PengSim.UI
```

Run the demo, press **A** to analyze it, then navigate or play back frames. Press **Q** to write `pengsim-profile.log` in the working directory. The report includes the rendered frame count, initial heatmap build time, average frame preparation time, and average and slowest output updates. Set `PENGSIM_PROFILE_PATH` to choose another file. Redirected runs profile one overview frame.

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
| `M` | Temporary playback shortcut for 240 iterations per second |
| `R` | Reset view |
| `Q` | Quit |

## Project structure

- `src/PengSim.BL/` — simulation logic
- `src/PengSim.UI/` — terminal visualization and demo
- [AI design log](src/Doc/ai_doc.md) — change history and design decisions
- [How rendering works](src/PengSim.UI/RENDERING.md) — short technical overview

## Simulation storage and limits

A run supports **65,000 penguins** and a board up to **65,000 columns × 65,000 rows**. Board width and height are both `ushort` and each must be between 1 and 65,000 inclusive. Penguin IDs are consecutive `ushort` values starting at 1. Zero denotes an empty cell in occupancy queries. Both offset coordinates (`X` for column and `Y` for row) are also `ushort` and start at zero. On the largest board the last valid coordinate on either axis is 64,999.

Each iteration stores an array of `PenguinPosition` values in ID order. To read a penguin’s position, use `engine.Iterations[iteration].Positions[id - 1]`. Each entry contains two 16-bit coordinates (4 bytes). For 1,000 penguins and 1,000 steps, the coordinate payload of the 1,001 snapshots is 4,004,000 bytes, plus collection overhead. Board area does not affect this payload.

Frame zero reflects placement through `AddPenguin` until the first `Step()`. Each step writes a fresh position array, so earlier frames remain available for read-only playback. A single occupied-position set serves collision checks while the demo moves penguins toward the center in ID order.
