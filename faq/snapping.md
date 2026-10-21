# How does the snapping work?

`T` cycles four modes, all of them the game's own snapping rather than a replacement for
it:

- **Auto** — what the game does on its own: the nearest pair of points wins. Pieces the
  game ships without snap points, such as standing torches and most decorations, do not
  snap here, just as in vanilla; the points this mod gives them are for Manual.
- **Manual** — you choose both points. The point you pick on the piece in hand lands
  exactly on the point you pick on the piece you are aiming at. Nothing is relative.
- **Grid** — Auto, plus a world grid. A piece that snaps to another piece snaps as in
  Auto; otherwise its position is rounded to the nearest grid point on the ground plane.
  Height is kept, and follows the terrain when the piece sits on it. The spacing is the
  `Grid Size` setting (0.5 m by default). Leveling and smoothing tools follow the grid
  too, also with their height held.
- **Off** — free placement, as the game's alternative placement key does. Terrain tools
  that support it then work at the cursor's height rather than at your feet.

In Manual, the game's own snap keys step through the points on the piece in hand, and
holding `Shift` with the same keys steps through the points on the piece you are aiming
at. Leave the target point alone and it uses whichever point is nearest your cursor. `G`
puts both back to automatic at once.

## Extra snap points

In Build and Edit mode, every piece gets points computed from the ones it already has:
edge midpoints and centers on flat pieces, face centers and edge midpoints on blocks,
ridges on roof pieces, and a grid across any face wider than `Grid Spacing` (2 m by
default, one floor tile; at least 0.5 m, and at most 16 steps per side). A point that
would fall inside the piece's own colliders is dropped, so a thin floor gets its center
but a solid block does not get a point buried in the middle of it.

Poles and beams are a special case. Both of their snap points sit on the center axis, so
the layout says nothing about how thick the piece is; the thickness is read from the
piece's colliders instead and the pole is treated as the thin box it is. That is what
gives you something to snap to on a pole's side rather than only at its ends. Corewood log
beams ship with six points, all at their two ends, and are recognized as beams too,
rather than taken for a thin wall.

Points are named with the game's own position words — Edge, Corner, Top, Top Center,
Bottom, Bottom Center, Side, Side Corner, End — and cycle in that order, pairing the top
and bottom of each edge or corner before moving to the next one. A pole's sides pair their
top, middle and bottom the same way; a beam lying down has its two ends.

Some pieces get hand-placed layouts instead: fences and palisades, with columns of points
at their ends and middle for meeting at any height, table and chest tops, item stands,
rugs, banners and the like. Torches and braziers with no points of their own get their
base and top. Each shape has its own switch in the config, the hand-placed layouts too, and
leveling and smoothing tools get a single point so you can snap a leveling pad to a
floor's edge before you flatten.

Like the rest of what the modes add, the extra points stay out of plain hammer building:
outside a mode the game snaps with each piece's own points, as it always has.
