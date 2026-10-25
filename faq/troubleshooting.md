# Why is something not working?

## Nothing happens when I press the mode key

Build mode unlocks at Building level 5 and Edit mode at 25, so a new character can't open
either. You also need a build piece selected for Build mode, or the Repair tool for Edit
mode; the Remove tool opens neither.

If that's not it, open `BepInEx/LogOutput.log` and look for `Loading [Valchitecture`. If
that line is missing, the mod isn't in the BepInEx folder the game is using.

## There are no rotation rings

Check that `Gizmo Visible` under `5. Tools` is on. If it is, the rings' graphics may have
failed to load; `BepInEx/LogOutput.log` says why. The rest of the mode still works.

## The build menu won't open

It stays closed while nudging has a piece frozen. Press the nudge key (`N`) again first.

## A piece that needs a workbench won't place

Below level 100, your character must stand inside the range of the right workbench, as in
the base game. The camera being inside it isn't enough. See "How do the build camera and
workbenches work together?"

## The build camera won't come out

It needs level 50, and a workbench to work from until level 100. If you entered the mode
away from any workbench, the camera stays with your character until you press `Alt + B`.

## Repairing won't fix an old piece

If `Stronger Pieces` was turned off after a piece was damaged or repaired with it on, the
piece can have more health than its normal maximum. The game then thinks it's undamaged
and won't repair it. This sorts itself out once the piece takes enough damage.

## My Building skill is leveling far too fast

Another mod is almost certainly speeding it up. Mods that change skill gain apply to this
skill like any other. Also check the server's `Skill Gain` under `3. Progression`. To see
what each action earned, set Mimir's `Verbosity` to `High` and check the log.
