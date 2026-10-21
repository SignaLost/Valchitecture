# Which mods does this replace or conflict with?

Valchitecture draws on four mods and replaces all of them. Remove these before running
it; two of them patching the same placement code is how you get a ghost that will not
move:

- **BuildCamera**
- **ComfyGizmo**
- **PerfectPlacement**
- **ExtraSnapPointsMadeEasy**

## Advanced Terrain Modifiers

Works alongside it. Valchitecture gives the terrain tools placement, reach, the camera
and a height lock; radius, shape and strength stay with Advanced Terrain Modifiers, and
the terrain tools deliberately ignore the tilt and spin modifiers so those inputs are
left alone.

## Workbench mods

Station range and extension reach are multipliers, set by the server in
`6. Building Rules`. They scale whatever a station already has, so a mod such as
AzuWorkbenchTweaks that sets a bench to 30 m still decides the starting point: at 2 that
bench reaches 60 m, and at 1 the mod's value stands as it is. The area where enemies
cannot spawn has its own multiplier, Base Range, applied the same way to the
size it already has; set it a little above Station Range for a margin past the build
limit. Anything else such a mod does, like crafting without a roof, is left to it.

AzuWorkbenchTweaks sets a station's values once, when the station loads, so a change to
its settings shows on stations that load afterward: rejoin the world, or move far enough
away for the area to unload and come back. Valchitecture's multipliers apply at once.

## Container mods

Building from containers runs on Chestworks, which handles AzuCraftyBoxes,
MultiUserChest and SeneaL UI; its FAQ covers how. In short, AzuCraftyBoxes and SeneaL
UI's own chest building are left in charge when they are on, and SeneaL UI's build panel
counts the containers Valchitecture builds from.

## Piece packs

Any mod that adds build pieces works. Extra snap points are computed from the snap points
a piece already has, so added pieces get them like the game's own. On every piece it
touches the mod names and orders all of the snap points, the piece's own included, so a
pack's own names are replaced by the same scheme as everything else. If you would rather
a piece were left exactly as its author made it, `Enable Per Piece` lists every piece
at the bottom of the config, grouped by the mod that adds it, with its snap point and
support switches side by side. It is off by default, because on a large modlist that is
thousands of config entries and it slows world load.

## Skill mods

The skill is registered under its own identifier, so it coexists with other mods that
add a Building skill. Mods that change skill gain rates apply to this one too, which is
usually what you want — but if your Building skill is climbing faster than it should,
that is where to look first.
