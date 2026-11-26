# How the map is drawn

The simulation stores the penguins’ axial map positions in ID order, with the origin at the hexagon's bottom-left corner and R growing upward. `HexFootprint` converts them to drawing coordinates centered on the world, with R growing downward. It does this by shifting the origin to the map center and flipping R. The sides may have different lengths. `HexRegion` describes the hex by a range for each of Q, R, and S = −Q − R, which fits every hexagon whose opposite sides are equal. The camera clamps to the screen rectangle around that region. The rectangle is a display concern only, and the simulation never sees it. Both terminal panels use rectangular windows. In either panel, a hex at `(q, r)` appears at column `2*q + r` and row `r`, leaving a blank column between neighbors.

## From a simulation to a frame

1. At startup and whenever playback selects a different iteration, `HexHeatmap.Rebuild` reads the saved positions and indexes the occupied playable cells. For each such cell, `HexGridScale.ToLevel` chooses the nearest bucket on each coarser lattice, preferring the innermost ring when two centers are equally close. The lattice is anchored at the map center, and the spacing between bucket centers doubles at every level. Each level stores counts only for occupied buckets. The outer buckets can be partial. A level's region is measured once at startup by converting the perimeter cells into buckets and taking the Q, R, and S ranges.
2. `FrameLayout` sizes the panels and chooses the overview level. The minimap stays at that level as the main view zooms in. At L0 the renderer reads occupancy from the selected frame’s occupied-cell set. Coarser views read the cached counts.
3. `FrameRenderer` draws the visible hexes, minimap selection, legend, and text into a character buffer. In an interactive terminal, `TerminalPresenter` compares the encoded rows with the previous frame and sends changed rows in one write.

Panning, zooming, and resizing reuse the selected iteration’s counts.
