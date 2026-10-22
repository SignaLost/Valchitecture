# Can I use this on a server that does not have the mod?

Yes. On a vanilla server the mod is client-side: join as you always have and keep the
modes, the gizmo, nudging, snapping, the build camera, the longer reach, area repair and
building from containers. Nobody else needs it, and a piece you place is an ordinary
piece to everyone else.

Two features turn themselves off there: **Stronger Pieces** and **Repair Upgrades**.
Both change a piece's health and support, which the game recomputes on whichever player
owns the piece; a player without the mod would restore the vanilla numbers, and on a
large build that means pieces collapsing for no visible reason. `valchitecture stats`
says when they are off.

The building rules under `6. Building Rules` stay at vanilla there as well, whatever your
own config says: station, extension and base range, no-build zones, biome limits,
clipping and extra support. They loosen rules the server enforces, so only a server
running the mod can change them. `valchitecture world` says when yours are held back.

A server that *does* run the mod requires it of every client, at the same minor version,
and all of these then apply to everyone.
