# How the map is drawn

The simulation stores the penguins’ offset positions in ID order. `HexFootprint` converts these positions to axial hex coordinates centered on the playable world. Both terminal panels use rectangular windows. In either panel, a hex at `(q, r)` appears at column `2*q + r` and row `r`, leaving a blank column between neighbors.

## From a simulation to a frame

1. At startup and whenever playback selects a different iteration, `HexHeatmap.Rebuild` reads the saved positions and indexes the occupied playable cells. For each such cell, `HexGridScale.ToLevel` rounds its position to the nearest bucket on each coarser lattice. The spacing between bucket centers doubles at every level. Each level stores counts only for occupied buckets.
2. `FrameLayout` sizes the panels and chooses the overview level. The minimap stays at that level as the main view zooms in. At L0 the renderer reads occupancy from the selected frame’s occupied-cell set. Coarser views read the cached counts.
3. `FrameRenderer` draws the visible hexes, minimap selection, legend, and text into a character buffer. In an interactive terminal, `TerminalPresenter` compares the encoded rows with the previous frame and sends changed rows in one write.

Panning, zooming, and resizing reuse the selected iteration’s counts.
