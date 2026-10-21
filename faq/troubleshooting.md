# Something is not working

## Nothing happens when I press the mode key

Build mode unlocks at Building level 10 and Edit mode at 25, so a fresh character cannot
open either. Check `BepInEx/LogOutput.log` for
`Loading [Valchitecture 1.0.0] (NullSignal.Valchitecture)`; if that line is missing, the DLL
is not in the BepInEx folder the game is launching.

## There are no rotation rings

The rings are drawn with their own shader, shipped as an asset bundle per platform. The
log says why a bundle did not load — usually it was built with a newer Unity than the
game runs. Everything else in the mode still works; only the rings are missing.

## The build menu will not open

It stays closed while you hold a piece in Edit mode or have one frozen with nudge, since
choosing another piece would drop the current one. Set the piece down or cancel first.

## A piece that needs a workbench will not place

Below level 100 you must stand inside a station of the right kind, as in the base game;
the camera being inside one is not enough. See the FAQ entry on the build camera and
workbenches.

## The build camera will not come out

It needs level 50 and a station to work from until level 100. If you entered the mode away
from any station, the camera waits on your character until you press the camera key.

## Repairing will not raise an old piece

A piece whose stored health is above the current maximum reads as undamaged, so the game
refuses the repair. This happens if the piece was damaged or repaired while **Stronger
Pieces** was on and the setting was turned off afterward. It sorts itself out once the
piece takes enough damage to fall below its vanilla maximum.

## My Building skill is leveling far too fast

Another mod is almost certainly adding to the gain. Mods that fortify skills or set
per-skill gain rates apply to this skill like any other, and they stack with the eased
early levels. Check the server's `Skill Gain` under `General` too. Set Mimir's
`Verbosity` to `High` and the log prints what each action awarded.
