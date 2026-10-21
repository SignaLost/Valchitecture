# Can I use this on a server that does not have the mod?

Yes. On a vanilla server the mod is client-side: join as you always have and keep the
modes, the gizmo, nudging, snapping, the build camera, the longer reach and area repair.
Nobody else needs it, and nobody else sees anything unusual — a piece you place is an
ordinary piece on the wire.

Two features turn themselves off there:

- **Stronger Pieces**
- **Repair Upgrades**

Both change a piece's health and how much support it loses at each joint, and the game
recomputes those on whichever player happens to own the piece. A player without the mod
would compute the vanilla numbers and quietly undo yours, which on a large build means
pieces collapsing for no visible reason. Standing the features down is the safe answer.
Run `valchitecture stats` in the console and it says when they are off.

The building rules a host can relax — station and extension range, no-build zones,
biome limits, clipping and extra support — stay at vanilla there as well, whatever your
own config says. They loosen rules the server enforces, so only a server running the mod
gets to change them.

A server that *does* run the mod requires it of every client, at the same minor version,
and all of these then apply to everyone.
