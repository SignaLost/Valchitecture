# How do I install and configure it?

## Installing

1. The easiest way is a mod manager, which also installs what Valchitecture needs: the
   BepInEx pack, **Jotunn**, **Chestworks** and **Mimir**. To install by hand, install
   those yourself, then unzip Valchitecture into its own folder in `BepInEx/plugins`.
2. Start the game once. This creates the settings file:
   `BepInEx/config/NullSignal.Valchitecture.cfg`.

Remove **BuildCamera**, **ComfyGizmo**, **PerfectPlacement** and
**ExtraSnapPointsMadeEasy** first, as Valchitecture won't work together with them.

## Multiplayer

If a server runs the mod, every player needs it too, and the first two numbers of the
version (like 1.1) must match. You can also play on a server without the mod; see "Can I
use this on a server that does not have the mod?"

## Settings

Changes to the settings apply right away, even while the game is running. Each setting's
description ends with `[Synced with Server]` or `[Not Synced with Server]`. On a server
with the mod, synced settings come from the server and only admins can change them. The
rest are yours.

| Section | What's in it | Synced |
|---|---|---|
| `1. General` | `Enabled` (on) | Yes |
| | `Verbosity` (how much goes in the log), `Show Hints` (on) | No |
| `2. Abilities` | A switch for each ability and for each skill bonus | Yes |
| `3. Progression` | How fast the skill levels, death loss, and unlock levels | Yes |
| `4. Keybinds` | Every key in the README's controls table | No |
| `5. Tools` | Camera speed, step sizes, grid size and the rotation rings | No |
| | `Camera Terrain Collision` (on) | Yes |
| `6. Snapping` | `Extra Snap Points` and a switch for each kind | Yes |
| | `Show Snap Points` (off) | No |
| `7. Building Rules` | Workbench ranges and relaxed building rules | Yes |
| `8. Per Piece Configs` | `Enable Per Piece` (off) | No |

The skill settings are explained in "What does the Building skill actually give me?", and
the snapping settings in "How does the snapping work?"

### Tools

| Setting | Does |
|---|---|
| `Camera Move Speed` | How fast the build camera flies (6 m/s) |
| `Camera Terrain Collision` | Stops the camera going through the ground (on) |
| `Rotation Step` | How far one scroll turns the piece (5°) |
| `Nudge Step` | How far one nudge moves the piece (0.05 m) |
| `Rotation World Axes` | Turn around the world's axes, not the piece's own (off) |
| `Grid Size` | Grid spacing in Grid snap mode (0.5 m) |
| `Gizmo Visible`, `Gizmo Fits Piece`, `Gizmo Scale`, `Gizmo Color X/Y/Z` | Show, size and color the rotation rings |

The step sizes are for step rate 1; the step rate keys make them smaller or larger.

### Building Rules

All of these are vanilla by default.

| Setting | Does |
|---|---|
| `Station Range` | Multiplies workbench build range (1) |
| `Extension Range` | Multiplies how far extensions can be from their workbench (1) |
| `Base Range` | Multiplies the area around a base where enemies can't spawn (1) |
| `Everything Supports` | Any piece can hold up pieces above it (off) |
| `Furniture Supports` | Furniture can hold up pieces above it (off) |
| `Ignore Clipping` | Pieces can be built inside each other (off) |
| `Ignore No-Build Zones` | Build in places the game normally forbids (off) |
| `Ignore Biome Limits` | Build biome-only pieces in any biome (off) |
| `Guard Used` | Nobody can move or remove a piece another player is using (on) |

### Per piece settings

Turn on `Enable Per Piece` to get settings for every single build piece, grouped by the
mod that adds it: a switch for its extra snap points and, for pieces that normally hold
nothing up, a switch to let them support others. It's off by default because large
modlists would add thousands of entries.

## Console

Type `valchitecture help` in the console to list the commands. `valchitecture stats` shows
your skill, abilities and ranges, `piece` the health and support of the piece you aim at,
`snap` the snap settings and that piece's snap points, and `world` the server's settings.
Mimir's `mimir` command can change the skill for testing; see "What does the Building
skill actually give me?"
