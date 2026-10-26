# How does the snapping work?

In Build or Edit mode, press `T` to switch between four snap modes:

- **Auto**: the game's normal snapping. The nearest snap points connect.
- **Manual**: you pick both points. The point you pick on the piece in hand lands exactly
  on the point you pick on the piece you aim at.
- **Grid**: snaps like Auto when a piece meets another piece. Otherwise the piece lines
  up on a grid, keeping its height. Set the spacing with `Grid Size` under `5. Tools`
  (0.5 m by default). Leveling and smoothing tools follow the grid too.
- **Off**: free placement, as when you hold the game's own key for it. Leveling and
  smoothing tools then work at your cursor's height instead of at your feet.

Some pieces, like standing torches and most decorations, have no snap points in the base
game, so they don't snap in Auto. The points this mod gives them work in Manual.

## Picking points in Manual

The game's own snap keys (`Q` `E`) step through the points on the piece in hand. Hold
`Shift` with the same keys to step through the points on the piece you aim at. Any point
you don't pick is chosen for you: the closest points connect, as in Auto. `G` resets both.

## Seeing snap points

Turn on `Show Snap Points` under `6. Snapping` to see the snap points on the piece in hand
and the piece you aim at, as small bubbles. They show through other pieces, a little
dimmer. In Manual, the points you've picked turn green. They show in Auto and Manual
only, never while holding a terrain tool, and in Auto the piece you aim at shows its
points only if the piece in hand has some.

## Extra snap points

In a mode, pieces get more snap points: the middle of each edge, centers, roof ridges,
points along poles and beams, and a grid across large floors and walls. The grid's
spacing is `Extra Snap Points: Grid Spacing`, 2 m by default (one floor tile). Set it to
0 to turn the grid off.

Some pieces get hand-placed points instead: fences, the tops of tables, chests and
workbenches, item stands, rugs, banners, seats and similar pieces. Torches get a point at
the base and top. Leveling and smoothing tools get one point, so you can snap a leveling
pad to a floor's edge before you flatten.

Each kind has its own switch under `6. Snapping`. Outside a mode, the extra points are
hidden and the game snaps as it always has.
