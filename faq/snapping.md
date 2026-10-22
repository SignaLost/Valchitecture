# How does the snapping work?

`T` cycles four modes in Build and Edit mode, all built on the game's own snapping:

- **Auto**: what the game does on its own; the nearest pair of points wins. Pieces the
  game ships without snap points, such as standing torches and most decorations, do not
  snap here, as in vanilla; the points this mod gives them are for Manual.
- **Manual**: you choose both points. The point you pick on the piece in hand lands
  exactly on the point you pick on the piece you are aiming at.
- **Grid**: Auto, plus a world grid. A piece that snaps to another piece snaps as in
  Auto; otherwise it moves to the nearest grid point on the ground plane, keeping its
  height, and following the terrain when it sits on it. The spacing is the `Grid Size`
  setting (0.5 m by default). Leveling and smoothing tools follow the grid too.
- **Off**: free placement, as holding the game's alternative placement key does.
  Leveling and smoothing tools then work at the cursor's height rather than at your feet.

In Manual, the game's own snap keys step through the points on the piece in hand, and
holding `Shift` with the same keys steps through the points on the piece you are aiming
at. Leave the target point alone and it uses whichever point is nearest your cursor. `G`
puts both back to automatic at once.

## Extra snap points

In a mode, every piece gets points computed from the ones it already has: edge midpoints
and centers on flat pieces, face centers and edge midpoints on blocks, ridges on roof
pieces, points along the sides of poles and beams, and a grid across any face wider than
`Extra Snap Points: Grid Spacing` (2 m by default, one floor tile; at least 0.5 m, and at
most 16 steps per side; 0 turns the grid off). A point that would fall inside the piece's
own colliders is dropped, so a thin floor gets its center but a solid block does not get
a point buried in the middle.

Some pieces get hand-placed layouts instead: fences and stakes, with columns of points at
their ends and middle for meeting at any height; table, chest and workbench tops; item
stands, rugs, banners, curtains, seats and the like. Torches and braziers with no points
of their own get their base and top, and leveling and smoothing tools get a single point,
so you can snap a leveling pad to a floor's edge before you flatten. Each shape has its
own switch under `5. Snapping`, the hand-placed layouts and the terrain tools too.

On every piece it touches, the mod names and orders all of the snap points, the piece's
own included: Top, Bottom, Center, Corner, Edge, Side and End, with the top and bottom of
each edge or corner paired so they cycle together.

Outside a mode, the extra points are hidden and the game snaps with each piece's own
points, as it always has.
