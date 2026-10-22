# Changelog

## 1.0.0

First release.

### Added

- **Building skill**, kept by Mimir: raised by placing, moving and repairing pieces and
  by removing pieces you did not build; removing your own pieces makes later placements
  pay nothing instead. Unlocks are permanent once earned. Each ability is described on
  Mimir's Skill Abilities page, with piece health and support loss among the perks.
- **Build mode** (Hammer's Call, level 10): rotation gizmo, nudging, snap modes, wider
  reach, copy piece (`Shift` or `Alt` + `Middle Mouse`), and a hint bar in the game's
  style. The game's alternative placement is held only with snapping Off.
- **Building from containers** (Gathered Hoard, level 20): placing a piece takes what the
  inventory lacks from containers within build reach; with Longhouse Stores (50), within
  range of the station covering the player or the piece; with Roots of the Hall (100),
  across stations linked by overlapping ranges. The build menu and requirement list count
  them. Runs on Chestworks as its `building` purpose.
- **Edit mode** (Huginn's Counsel, level 25): pick up a built piece and set it down
  elsewhere with its contents and data, under the game's removal and placement checks.
- **Area repair** (Eir's Touch, level 35): one repair swing repairs every damaged piece
  around the player, as far as stamina allows, at half the cost per piece.
- **Build camera** (Raven's View, level 50): a detached camera within the working
  station's range or a range around the character. With Bifröst Step (75) it moves
  between stations; with Raven's Reach (100) pieces are built where it is and no station
  is needed.
- **Removal pickup** (Dvergr Thrift, level 100): a removed piece's materials go to
  containers already holding them, then to the inventory; the rest drops as usual.
- **Snapping modes**: Auto (the game's), Manual (exact chosen point pairs), Grid (Auto,
  with unsnapped pieces on a world grid) and Off (the game's alternative placement).
- **Extra snap points** in a mode, computed from each piece's own, plus hand-placed
  layouts for fences, furniture, item stands and similar pieces, each shape with a switch.
  Outside a mode the game keeps each piece's own points.
- **Site work**: a height lock for leveling and smoothing tools, and a snap point so
  they can snap to a floor edge.
- **Piece strength**: pieces store the builder's skill, gaining health and losing less
  support per joint, and stamina per build swing drops to half at level 100. Repairing
  raises a piece to the repairer's skill.
- **Building rules** a server can relax, vanilla by default: station, extension and base
  range multipliers; building in no-build zones and outside a piece's biome; pieces inside
  others; support from all pieces, from flat-topped furniture, or from chosen pieces.
  `Guard Used` (on) keeps a piece another player is using, such as a chest they have
  open or a bed they sleep in, from being removed or moved.
- **Settings**: a switch and an unlock level for each ability, `Skill Gain`, and switches
  for `Longer Reach`, `Stronger Pieces`, `Cheaper Swings` and `Repair Upgrades`.
- **Per piece settings** (`Enable Per Piece`): snap point and support switches for every
  piece, grouped by source mod.
- `valchitecture` console command: skill stats, the aimed piece, snap analysis and server
  settings.
- `ValchitectureAPI` for other mods: modes, camera, snap mode and piece state.
- Translation keys for all text, the config window included.
- **Vanilla servers**: everything works client-side except Stronger Pieces, Repair
  Upgrades and the building rules, which stay at vanilla there.
