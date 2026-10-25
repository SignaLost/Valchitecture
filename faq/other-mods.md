# Which mods does this conflict with?

Valchitecture is inspired by four mods and won't work together with them, because they
change the same parts of the game. Remove them first:

- **BuildCamera**
- **ComfyGizmo**
- **PerfectPlacement**
- **ExtraSnapPointsMadeEasy**

## Terrain mods

The tilt and spin keys (`Shift` or `Alt` + scroll) do nothing on terrain tools, so a
terrain mod that uses them to change brush size or shape keeps working.

## Workbench mods

`Station Range`, `Extension Range` and `Base Range` under `7. Building Rules` multiply
whatever range a workbench already has, including a range another mod gave it. At 2, a
workbench another mod set to 30 m reaches 60 m. At 1, the other mod's range stays as it
is. Anything else that mod does is left to it.

Tip: set `Base Range` a little above `Station Range`, so enemies can't spawn right at the
edge of where you can build.

## Container mods

Chests are handled by Chestworks, which works with AzuCraftyBoxes, MultiUserChest and
SeneaL UI; its FAQ has the details. In short, if AzuCraftyBoxes or SeneaL UI's own chest
building is turned on, it stays in charge, and SeneaL UI's build panel counts the chests
Valchitecture builds from.

## Piece packs

Pieces from any mod work, and get extra snap points just like the game's own. To leave a
mod's pieces exactly as their author made them, turn on `Enable Per Piece` under
`8. Per Piece Configs` and switch off their snap points.

## Skill mods

Valchitecture's Building skill doesn't clash with other mods that add a Building skill.
Mods that change how fast skills level affect this one too, so if your Building skill
climbs faster than it should, check those first.
