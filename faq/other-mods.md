# Which mods does this replace or conflict with?

Valchitecture draws on four mods and replaces all of them. Remove them before running
it; two mods patching the same placement code is how you get a ghost that will not move:

- **BuildCamera**
- **ComfyGizmo**
- **PerfectPlacement**
- **ExtraSnapPointsMadeEasy**

## Terrain mods

Valchitecture gives the terrain tools placement, reach, the camera and a height lock.
The tilt and spin modifiers do nothing on them, so a terrain mod that uses a modifier
plus scroll for radius or shape keeps those inputs.

## Workbench mods

`Station Range`, `Extension Range` and `Base Range` under `6. Building Rules` are
multipliers on whatever a station already has, so a mod that gives a bench its own range
still sets the starting point: at 2, a bench another mod set to 30 m reaches 60 m, and
at 1 that mod's value stands as it is. `Base Range` scales the area where enemies cannot
spawn the same way; set it a little above `Station Range` for a margin past the build
limit. Anything else such a mod does, like crafting without a roof, is left to it.

## Container mods

Building from containers runs on Chestworks, which handles AzuCraftyBoxes,
MultiUserChest and SeneaL UI; its FAQ covers how. In short, AzuCraftyBoxes and SeneaL
UI's own chest building are left in charge when they are on, and SeneaL UI's build panel
counts the containers Valchitecture builds from.

## Piece packs

Any mod that adds build pieces works. Extra snap points are computed from the snap points
a piece already has, so added pieces get them like the game's own, and their points are
renamed by the same scheme as everything else. To leave a piece exactly as its author
made it, turn on `Enable Per Piece`, which lists every piece at the bottom of the config,
grouped by the mod that adds it, with its snap point and support switches.

## Skill mods

The skill is registered under its own identifier, so it coexists with other mods that
add a Building skill. Mods that change skill gain rates apply to this one too; if your
Building skill is climbing faster than it should, that is where to look first.
