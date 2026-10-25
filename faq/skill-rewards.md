# What does the Building skill actually give me?

## Leveling up

The skill rises when you place pieces, move them in Edit mode, repair them, and remove
pieces someone else built, like ruins or another player's base. Placing earns the most.
Removing your own pieces earns nothing, and each one you remove makes a later placement
earn nothing too, so building and tearing down the same piece won't level you.

## Abilities

| Level | Ability | What it does |
|---|---|---|
| 5 | Hammer's Call | Build mode |
| 15 | Gathered Hoard | Building takes materials from chests within reach |
| 25 | Huginn's Counsel | Edit mode: move built pieces |
| 35 | Eir's Touch | Area repair, 5 m around you, growing to 25 m |
| 50 | Raven's View | The build camera |
| 50 | Longhouse Stores | Chests in your workbench's range count |
| 75 | Bifröst Step | The camera can move between workbenches |
| 75 | Roots of the Hall | Linked workbenches share their chests |
| 85 | Dvergr Thrift | A removed piece's materials come back to you |
| 100 | Raven's Reach | Pieces go where the camera is |

An ability stays unlocked once you reach its level, so dying never takes one away.

## What it does to what you build

These bonuses grow a little as you level, up to these values at level 100:

| Bonus | At level 100 | Setting |
|---|---|---|
| Piece health | Double | `Stronger Pieces` |
| Support lost from piece to piece | 40% less | `Stronger Pieces` |
| Stamina per hammer swing | Half | `Cheaper Swings` |
| Build reach in a mode | Double | `Longer Reach` |

A piece remembers the skill it was built with. Repairing it raises that to your skill
(the `Repair Upgrades` setting), and never lowers it.

## Settings

A server sets how the skill progresses under `3. Progression`:

| Setting | Does |
|---|---|
| `Skill Gain` | How fast the skill levels (1); 2 is twice as fast |
| `Death Loss` | How much of the usual death penalty the skill takes (1); 0 means none |
| `Fortify Building` | When on, a death drops the skill only a few levels (off) |

With `Fortify Building` on, a death drops the skill at most Mimir's `Max Gap` (12 levels
by default), and `Death Loss` is ignored.

The same section has the level each ability unlocks at, such as `Hammers Call Level`
(spelled without apostrophes). The levels above are the defaults.

Under `2. Abilities` there is a switch for each ability and each bonus above. Switching an
ability off doesn't take away its unlock; it works again when you switch it back on.

## Mimir

The skill is kept by **Mimir**. Mimir's `Unlock Notices` setting chooses how new
abilities are announced: the game's unlock popup (the default), in the middle of the
screen, in the corner, or not at all. Mimir's Skill Abilities page in the compendium
lists every ability and what it does.

For testing, with devcommands on, `mimir level building 50` sets your level and
`mimir help` lists the other commands.
