# Changelog

## 1.0.0

First release.

### Added

- **Building skill**, raised by placing, moving and repairing pieces, and by removing
  pieces you did not build. Placing right after removing your own pieces pays nothing,
  like the game's remove debt for Crafting. Unlocks are permanent once earned.
  Each ability has a description on Mimir's Skill Abilities compendium page, and piece
  health and support loss show among the skill's perks there.
- **Build mode** (Hammer's Call, level 10): rotation gizmo, nudging, wider reach, and a hint bar cloned
  from the game's build hints.
- **Edit mode** (Huginn's Counsel, level 25): pick up a built piece and set it down elsewhere, with its
  contents and data. Uses the game's removal checks; open containers and occupied ships
  cannot be moved.
- **Area repair** (Eir's Touch, level 35): one repair swing repairs every damaged piece around the
  player, as far as stamina allows.
- **Building from containers** (Gathered Hoard, level 20): placing a piece takes what the
  inventory lacks from containers within build reach; with Longhouse Stores (level 50),
  within range of the station covering the player or the piece; with Roots of the Hall
  (level 100), across stations linked by overlapping ranges.
  The build menu and requirement list count the containers. Runs on Chestworks, a new
  dependency, as its `building` purpose.
- **Removal pickup** (Dvergr Thrift, level 100): a removed piece's materials go to containers already
  holding them, then to the inventory within its space and carry weight. The rest drops as
  usual.
- **Build camera** (Raven's View, level 50): a detached camera limited to the working
  station's range and a range around the character. With Bifröst Step (level 75) it can
  move between stations; with Raven's Reach (level 100) pieces are built where it is.
- **Snapping modes**: Auto (the game's), Manual (exact chosen point pairs), Grid (Auto,
  with unsnapped pieces aligned to a world grid) and Off (the game's alternative placement).
- **Extra snap points** in a mode, computed from each piece's own points, plus hand-placed
  layouts for fences, tables, chests, item stands and similar pieces.
- **Site work**: a height lock for leveling and smoothing tools.
- **Piece strength**: pieces store the builder's skill, gaining health and losing less
  support per joint. Repairing raises a piece to the repairer's skill.
- **Building rules** a server can relax, vanilla by default: station, extension and base
  range multipliers; building in no-build zones and outside a piece's biome; pieces inside
  others; support from all pieces, from flat-topped furniture, or from chosen pieces.
- **Ability settings**: a switch and an unlock level for each ability, and `Skill Gain`
  for how fast the skill levels.
- **Per piece settings** (optional): snap point and support switches for each piece,
  grouped by source mod.
- `valchitecture` console command: skill, aimed piece, snap analysis and server settings.
- `ValchitectureAPI` for other mods: modes and pieces. The Building skill and its
  unlocks are Mimir's, and container stock is Chestworks' `building` purpose.
- Translation keys for all text, the config window included.

### Changed

- In a mode, the game's alternative placement is held only with snapping Off, and copy
  piece moves to `Shift` or `Alt` + `Middle Mouse Button`.
- Stamina per build swing drops with skill, to half at level 100.
- From level 100, a station whose range reaches within 5 m of the detached camera counts
  as in range. With the character outside every station, pieces that need one can then
  only be placed inside a station's range.
- Extra snap points are hidden outside a mode, so plain hammer building keeps each piece's
  own points.
- A piece another player is using cannot be removed or moved: a container they have open,
  a cart they are pulling, a seat they sit in or a bed they sleep in. The game already
  refuses ships with anyone aboard. `Guard Used` off restores the game's rules.
