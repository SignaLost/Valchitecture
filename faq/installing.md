# How do I install and configure it?

## Installing

1. Install it through a mod manager, which installs the BepInEx pack, **Jotunn**,
   **Chestworks** and **Mimir** with it; or install those yourself and unzip the package
   into a folder of its own in `BepInEx/plugins`.
2. Launch once to create `BepInEx/config/NullSignal.Valchitecture.cfg`.

Remove **BuildCamera**, **ComfyGizmo**, **PerfectPlacement** and
**ExtraSnapPointsMadeEasy** first; Valchitecture replaces them.

## Multiplayer

A server running the mod, dedicated or hosted, requires it of every client at the same
minor version. On a vanilla server the mod is client-side; see the FAQ entry on vanilla
servers for what turns off there.

## Configuration

Settings reload live when the file changes. Each setting's description ends with
`[Synced with Server]` or `[Not Synced with Server]`. On a server running the mod, the
server's values apply for synced settings and only admins can change them; the rest
are each player's own.

| Section | Settings | Synced |
|---|---|---|
| `1. General` | `Enabled`; `Skill Gain` (1, 0.1 to 5; 2 is twice as fast); `Longer Reach`, `Stronger Pieces`, `Cheaper Swings`, `Repair Upgrades` (all on) | Yes |
| | `Verbosity` (`Low`), `Show Hints` (on) | No |
| `2. Abilities` | A switch and an unlock level for each ability; see the skill rewards entry | Yes |
| `3. Keybinds` | Every key in the README's table | No |
| `4. Tools` | `Camera Move Speed` (6 m/s, 1 to 60); `Rotation Step` (5°, 0.1 to 45) and `Nudge Step` (0.05 m, 0.01 to 1) at step rate 1; `Rotation World Axes` (off); `Grid Size` (0.5 m, 0.5 to 2); `Gizmo Visible`, `Gizmo Fits Piece`, `Gizmo Scale`, `Gizmo Color X/Y/Z` | No |
| | `Camera Terrain Collision` (on) | Yes |
| `5. Snapping` | `Extra Snap Points` and a switch per shape; `Extra Snap Points: Grid Spacing` (2 m) | Yes |
| `6. Building Rules` | `Station Range`, `Extension Range`, `Base Range` (multipliers, 1); `Everything Supports`, `Furniture Supports`, `Ignore Clipping`, `Ignore No-Build Zones`, `Ignore Biome Limits` (all off); `Guard Used` (on): pieces another player is using cannot be moved or removed | Yes |
| `7. Per Piece Configs` | `Enable Per Piece` (off) | No |

Sections are numbered so config managers list them in order.

`Enable Per Piece` adds a section for every build piece, grouped by the mod that adds it:
a snap point switch (not synced) and, for pieces that do not support others, a support
switch (synced). It is off by default, as large modlists produce thousands of entries.

## Console

`valchitecture help` lists the subcommands: `stats` (your skill, abilities and ranges),
`piece` (health, support and build level of the aimed piece), `snap` (snap settings and
the aimed piece's snap points) and `world` (every server-decided setting). Each readout
also goes to the log file. Mimir's `mimir` command changes the skill and its unlocks for
testing; see the skill rewards entry.
