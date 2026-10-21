# Valchitecture

Precision building for Valheim, earned through a **Building** skill. One key opens a
building mode with gizmo rotation, fine nudging, exact snapping and a hint bar in the
game's own style. As the skill grows it adds moving built pieces, area repair, a build
camera, and the reach to build whole structures from the air.

Valchitecture replaces **BuildCamera**, **ComfyGizmo**, **PerfectPlacement** and
**ExtraSnapPointsMadeEasy**; do not run it alongside them. It needs **Chestworks** and
**Mimir**, which a mod manager installs with it.

## Progression

The skill rises as you place, move and repair pieces, and clear structures you did not
build, at about the pace of the game's own skills. Each unlock is announced when you earn
it.

| Level | Ability |
|---|---|
| **10** | **Hammer's Call**: Build mode, with gizmo, nudging, snapping and wider reach |
| **20** | **Gathered Hoard**: materials come from chests within reach |
| **25** | **Huginn's Counsel**: Edit mode, to pick up a built piece and put it somewhere better |
| **35** | **Eir's Touch**: one swing repairs everything nearby |
| **50** | **Raven's View**: the build camera leaves your character and flies around your workbench |
| **50** | **Longhouse Stores**: chests in your workbench's range count |
| **75** | **Bifröst Step**: the camera can move from one workbench to the next |
| **100** | **Raven's Reach**: pieces go where the camera is |
| **100** | **Roots of the Hall**: linked workbenches share their chests |
| **100** | **Dvergr Thrift**: a removed piece's materials come back to you |

The skill also improves what you build: more health, less support lost at each joint,
and cheaper hammer swings. Repairing a piece brings it up to your current skill, so an old
base never needs rebuilding. Unlocks are permanent once earned, so dying never takes one
away.

## Features

**Rotation.** Three rings on the piece. Scroll to turn it, with modifiers to tilt it
toward you or spin it in the plane you face. Reset the rotation, or copy it from the
piece you are aiming at.

**Nudge.** Freezes the piece and moves it in small steps in any direction. One step size
scales nudging and rotation together.

**Site work.** For leveling and smoothing tools, nudge becomes a height lock: the tool
follows the cursor while its height is held.

**Snapping.** Cycles **Auto** (the game's own), **Manual**, **Grid** and **Off**. Manual
is exact: the point you pick on the piece in hand lands on the point you pick on the piece
you aim at. Grid snaps like Auto and lines everything else up on a world grid. Pieces get
extra snap points, including grids across large floors and walls.

**Edit mode.** The mode key with the hammer's Repair tool selected. Click a built piece to
pick it up, click again to set it down, right-click to put it back. The piece keeps
everything it held.

**Copy.** Copy the piece under the crosshair, with its rotation or keeping your own.

**Area repair.** One repair swing repairs every damaged piece around you, nearest first,
as far as your stamina allows, at half the cost per piece.

**Building from containers.** Building takes what your inventory lacks from nearby chests,
carts and ships, and the build menu counts them. The reach grows from your own to your
workbench's range, and at level 100 to every workbench whose range touches it.

**Removal pickup.** At level 100, a removed piece's materials go into chests that already
hold them, then into your inventory. Only what does not fit is dropped.

**Build camera.** Entering a mode lifts the camera off your shoulder. Fly it with your
movement keys and sprint to go faster. Rings on the ground show the limits in force.

**Building rules.** Server options, all vanilla by default: larger station, extension
and base ranges, building in no-build zones and outside a piece's biome, pieces inside
others, and support from pieces that normally hold nothing up.

## Controls

All rebindable in the config file. The hint bar shows what applies at the moment.

| Action | Default |
|---|---|
| Build / Edit mode | `Shift + B` |
| Build camera on / off | `Alt + B` |
| Camera move, up, down, fast | movement keys, `Space`, `Left Ctrl`, sprint |
| Turn, tilt, spin | `Scroll`, `Shift + Scroll`, `Alt + Scroll` |
| Reset rotation, copy the aimed piece's rotation | `V`, `C` |
| Nudge on / off | `N` |
| Nudge | `← → ↑ ↓`, `Shift + ↑ ↓` for up and down |
| Step size | `-` `+`, `Shift` for jumps of 10 |
| Snap mode | `T` |
| Snap point on the piece in hand | the game's own keys (`Q` `E`) |
| Snap point on the piece aimed at | `Shift` + the same |
| Both snap points back to automatic (Manual) | `G` |
| Copy, copy without rotation | `Shift + Middle Mouse Button`, `Alt + Middle Mouse Button` |
| Edit: pick up / set down, put back | `Left Click`, `Right Click` |

## More

The FAQ covers installing, multiplayer, how the build camera and workbenches interact,
snapping, containers, the skill's numbers, other mods, the API for other mods,
translating and building from source.

## Credits

- **Searica**: *ExtraSnapPointsMadeEasy*, the extra snap point engine this grew from, its
  hand-placed layouts, and the hint bar technique.
- **ComfyMods / Rolopogo**: *ComfyGizmo*, the gizmo's design.
- **Azumatt**: *BuildCamera (Custom Hammers Edition)*, the detached camera.
- **jpw1991 / Searica and contributors**: *PerfectPlacement*, nudging and moving built pieces.
- **Blaxxun**: *Building*, the skill icon, and the reference for pieces remembering their skill.

## License

GNU General Public License v3.0; see [LICENSE.md](LICENSE.md). Parts of the snap point
code and the hand-placed layouts come from ExtraSnapPointsMadeEasy, under the same
license.
