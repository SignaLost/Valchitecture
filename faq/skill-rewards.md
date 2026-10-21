# What does the Building skill actually give me?

The skill is raised by placing pieces (1 point), moving them (0.75), repairing them (0.125)
and removing pieces someone else built, such as ruins or another player's base (0.5), in
the same units the game pays for a sword swing or a crafted item. Removing your own pieces
pays nothing, and each one (up to 20) makes a later placement pay nothing, as the game does
for Crafting, so placing and removing the same piece earns nothing. The earliest levels
are eased so Build mode arrives after roughly 220 pieces, while level 100 costs about what
a vanilla skill does.

## Unlocks

| Level | Ability | Id | What it does |
|---|---|---|---|
| 10 | Hammer's Call | `build` | Build mode |
| 20 | Gathered Hoard | `supply` | Building from containers within your build reach |
| 25 | Huginn's Counsel | `edit` | Edit mode |
| 35 | Eir's Touch | `mend` | Area repair, 5 m growing to 25 m by level 99 |
| 50 | Raven's View | `camera` | The build camera; build reach starts growing |
| 50 | Longhouse Stores | `workshop` | Containers in station range |
| 75 | Bifröst Step | `hop` | The camera moves from one workbench to the next |
| 100 | Raven's Reach | `reach` | Pieces go where the camera is |
| 100 | Roots of the Hall | `network` | Containers across linked workbenches |
| 100 | Dvergr Thrift | `salvage` | Removal pickup |

An ability unlocks when your level reaches it and stays unlocked, so dying never takes one
away. Ranges that grow with the skill, such as the camera's and area repair's, follow your
current level.

These are the defaults. A server can change them under `Abilities`, where each ability
has a switch to turn it off and a level to unlock it at (`Hammer's Call` and
`Hammer's Call Level`, and so on; the `.cfg` file spells them without apostrophes, as
BepInEx requires). An ability switched off keeps its unlock and works again when switched
back on. The build camera's range and the longer build reach grow from Raven's View's
level, and area repair's radius from Eir's Touch's.

`Skill Gain` under `General` sets how fast the skill levels (2 is twice as fast), next to
the switches for the skill's other effects: `Longer Reach`, `Stronger Pieces`,
`Cheaper Swings` and `Repair Upgrades`.

Abilities earned at the same moment are announced together as "New abilities unlocked",
each listed in the corner.

The skill and its unlocks are kept by **Mimir**. Its `Unlock Notices` setting chooses how
unlocks are announced: mid-screen (the default), in the corner, or not at all. With
devcommands on, `mimir level building 50`, `mimir unlock building camera` and
`mimir reset building all` change the skill and unlocks for testing; `mimir help` lists the
rest.

## What it does to what you build

| At level 100 | Change |
|---|---|
| Piece health | Double — 2% more every second level |
| Support lost at each joint | 0.6x — 1.6% less every fourth level |
| Stamina per hammer swing | Half — 2% less every fourth level |
| Build reach in a mode | Double — 20% more every tenth level from 60 |

A piece stores the skill it was built with, not the results, so these numbers can be
retuned without touching an existing world. Repairing a piece raises the stored skill to
the repairer's, and never lowers it.
