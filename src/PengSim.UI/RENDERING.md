# How the map is drawn

The simulation stores occupancy by column and row. `HexFootprint` converts these positions to axial hex coordinates centered on the playable world. Both terminal panels use rectangular windows. In either panel, a hex at `(q, r)` appears at column `2*q + r` and row `r`, leaving a blank column between neighbors.

## From a simulation to a frame

1. At startup, `HexHeatmap.Rebuild` scans the playable cells. For each occupied cell, `HexGridScale.ToLevel` rounds its position to the nearest bucket on each coarser lattice. The spacing between bucket centers doubles at every level. Each level stores counts only for occupied buckets.
2. `FrameLayout` sizes the panels and chooses the overview level. The minimap stays at that level as the main view zooms in. At L0 the renderer reads occupancy directly from the grid. Coarser views read the cached counts.
3. `FrameRenderer` draws the visible hexes, minimap selection, legend, and text into a character buffer. In an interactive terminal, `TerminalPresenter` compares the encoded rows with the previous frame and sends changed rows in one write.

The demo displays a static iteration. Panning, zooming, and resizing reuse its counts. If an iteration changes occupancy, rebuild the heatmap before drawing that iteration so the overview and L0 show the same population.
