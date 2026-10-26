# Valchitecture

Precision building for Valheim, earned through a new **Building** skill. One key opens
Build mode, where you can turn pieces any way you like, nudge them into place and snap
them exactly where you want. As the skill grows you unlock more: moving built pieces,
repairing everything around you at once, building with materials from your chests, and a
camera that flies around your base.

Valchitecture is inspired by **BuildCamera**, **ComfyGizmo**, **PerfectPlacement** and
**ExtraSnapPointsMadeEasy**, and won't work together with them.

## Progression

The skill rises as you place, move and repair pieces, and as you tear down structures you
didn't build. Each ability is announced when you earn it, and it stays yours: dying never
takes one away.

| Level | Ability | What you get |
|---|---|---|
| 5 | **Hammer's Call** | Build mode: rotation rings, nudging, snapping and longer reach |
| 15 | **Gathered Hoard** | Building takes missing materials from nearby chests |
| 25 | **Huginn's Counsel** | Edit mode: pick up a built piece and put it somewhere better |
| 35 | **Eir's Touch** | One repair swing repairs everything nearby |
| 50 | **Raven's View** | The build camera leaves your character and flies around your workbench |
| 50 | **Longhouse Stores** | Chests in your workbench's range count |
| 75 | **Bifröst Step** | The camera can move from one workbench to the next |
| 75 | **Roots of the Hall** | Linked workbenches share their chests |
| 85 | **Dvergr Thrift** | A removed piece's materials come back to you |
| 100 | **Raven's Reach** | Pieces go where the camera is |

The skill also makes what you build better: pieces get more health and lose less support,
and hammer swings cost less stamina. Repairing an old piece brings it up to your current
skill, so you never need to rebuild a base.

## Features

**Rotation.** Three rings around the piece. Scroll to turn it, hold `Shift` to tilt it
toward you, or `Alt` to spin it. Turning always lands exactly straight when it passes a
right angle. Reset straightens the piece to face the way you're looking, or copy the
rotation from the piece you are aiming at.

**Nudge.** Freezes the piece so you can move it in small steps in any direction. With
leveling and smoothing tools, it holds the height while the tool follows your cursor.

**Snapping.** Switch between **Auto** (the game's own), **Manual** (you pick the exact
points that connect), **Grid** (lines pieces up on a grid) and **Off**. Pieces get extra
snap points while you are in a mode, including grids across large floors and walls.

**Edit mode.** With the hammer's Repair tool selected, press the mode key. Click a built
piece to pick it up, click again to set it down, or right-click to put it back. The piece
keeps everything it held.

**Copy.** Copy the piece you are aiming at, with its rotation or keeping your own.

**Area repair.** One repair swing repairs every damaged piece around you, nearest first,
as far as your stamina allows, at half the cost per piece.

**Building from chests.** Building takes what your inventory lacks from nearby chests,
carts and ships, and the build menu counts them. Later levels reach every chest in your
workbench's range, and then across linked workbenches.

**Materials back.** At level 85, a removed piece's materials go into chests that already
hold them, then into your inventory. Only what doesn't fit drops on the ground.

**Build camera.** Entering a mode lifts the camera off your shoulder. Fly it with your
movement keys and sprint to go faster. Rings on the ground show how far it can go.

**Building rules.** Server owners can relax the game's building rules: bigger workbench
ranges, building in no-build zones or outside a piece's biome, pieces inside each other,
and more support. Everything stays vanilla unless they change it.

## Controls

Every key can be changed in the settings. The hint bar shows the keys you can use.

| Action | Default |
|---|---|
| Build / Edit mode | `Shift + B` |
| Build camera on / off | `Alt + B` |
| Camera move, up, down, fast | movement keys, `Space`, `Left Ctrl`, sprint |
| Turn, tilt, spin | `Scroll`, `Shift + Scroll`, `Alt + Scroll` |
| Reset rotation, copy the aimed piece's rotation | `V`, `C` |
| Nudge on / off | `N` |
| Nudge | `← → ↑ ↓`, `Shift + ↑ ↓` for up and down |
| Smaller / larger rotation steps (nudge steps while nudging) | `-` `+` |
| Rotation steps while nudging | `Shift` + `-` `+` |
| Snap mode | `T` |
| Snap point on the piece in hand | the game's own keys (`Q` `E`) |
| Snap point on the piece aimed at | `Shift` + the same |
| Reset both snap points (Manual) | `G` |
| Copy, copy without rotation | `Shift + Middle Mouse`, `Alt + Middle Mouse` |
| Edit: pick up / set down, put back | `Left Click`, `Right Click` |

## More

The FAQ answers common questions: installing and settings, playing on servers without the
mod, the build camera and workbenches, snapping, Edit mode, building from chests, the
skill, other mods, translating and fixing problems.

## For mod authors

Other mods can read Valchitecture's state through `ValchitectureAPI`, a public static
class in Valchitecture.dll. Everything in it is for the local player, is read on the main
thread, and returns its "off" value before a world loads. `ValchitectureAPI.Version` rises when members are added; existing members
keep their meaning.

### Members

| Member | Gives |
|---|---|
| `Version`, `Guid` | The API version, and the plugin GUID (`NullSignal.Valchitecture`) |
| `Enabled` | The mod's `Enabled` setting |
| `SkillId` | The Building skill's id in Mimir, `valchitecture.building` |
| `InBuildMode`, `InEditMode`, `CameraDetached` | The current mode and camera |
| `CurrentSnapMode` | `Auto`, `Manual`, `Grid` or `Off` |
| `BuiltAtLevel(wearNTear)` | The level a piece was built or last upgraded at, 0 for none |
| `IsInUseByOther(piece)` | Another player is using the piece: an open container, a cart being pulled, a seat or a bed; what `Guard Used` refuses to remove |

### The skill

The Building skill and its abilities are kept by **Mimir**. `MimirAPI.Find("building")`
gives its level and abilities, and the readers that take names, such as
`MimirAPI.IsUnlocked("building", "edit")`, work through reflection without a reference.
The abilities, in default level order: `build`, `supply`, `edit`, `mend`, `camera`,
`workshop`, `hop`, `reach`, `network`, `salvage`; the skill rewards entry has their
names. Each one's full id is the skill's and its own, such as
`valchitecture.building.camera`. A server can move any of them, so read levels from
Mimir rather than assuming these. Mimir's FAQ has the guide.

### Container stock

Building's container stock lives in **Chestworks**, as its `building` purpose:
`ChestworksAPI.Stock("building", item)` is what building can draw on beyond the
inventory, and `ChestworksAPI.IsActive("building")` whether it draws on containers right
now. Chestworks' FAQ has the guide.

### Using it without a reference

```csharp
if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("NullSignal.Valchitecture", out var info))
{
    var api = info.Instance.GetType().Assembly.GetType("Valchitecture.ValchitectureAPI");
    var building = api?.GetProperty("InBuildMode");
    bool inBuildMode = building != null && (bool)building.GetValue(null);
}
```

## Credits

- **Searica**: *ExtraSnapPointsMadeEasy*, the extra snap point engine this grew from, its
  hand-placed layouts, and the hint bar technique.
- **ComfyMods / Rolopogo**: *ComfyGizmo*, the gizmo's design.
- **Azumatt**: *BuildCamera (Custom Hammers Edition)*, the detached camera.
- **jpw1991 / Searica and contributors**: *PerfectPlacement*, nudging and moving
  built pieces.
- **Blaxxun**: *Building*, the skill icon, and the reference for pieces that remember
  the skill they were built at.
- **MidnightsFX**: *Impactful Skills*, the inspiration for a skill whose abilities unlock
  as it levels.

## License

GNU General Public License v3.0; see [LICENSE.md](LICENSE.md). Parts of the snap point
code and the hand-placed layouts come from ExtraSnapPointsMadeEasy, under the same
license.
