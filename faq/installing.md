# How do I install and configure it?

## Installing

1. Install the BepInEx pack for Valheim, **Jotunn**, **Chestworks** and **Mimir**.
2. Install it through a mod manager, which installs the others for you, or unzip the
   package into a folder of its own in `BepInEx/plugins`.
3. Launch once to create `BepInEx/config/NullSignal.Valchitecture.cfg`.

Remove **BuildCamera**, **ComfyGizmo**, **PerfectPlacement** and
**ExtraSnapPointsMadeEasy** first; Valchitecture replaces them.

## Configuration

Settings reload live when the file changes. Each setting's description ends with
`[Synced with Server]` or `[Not Synced with Server]`:

- **Synced** settings affect how everyone plays: the mod switch, abilities, progression, building rules
  and snap points. On a server running the mod, the server's values apply and only admins
  can change them.
- **Not synced** settings are each player's own: keys, hints, the gizmo and camera speed.

Sections are numbered so config managers list them in order. `Enable Per Piece`, at the
bottom, adds a snap point and a support switch for every build piece, grouped by the mod
that adds it. It is off by default, as large modlists produce thousands of entries.

## Multiplayer

A server running the mod, dedicated or hosted, requires it of every client, at the same
minor version. On a vanilla server the mod is client-side; see the FAQ entry on vanilla
servers for what turns off there.
