# How does building from containers work?

From level 15 (Gathered Hoard), any materials missing from your inventory when you build
are taken from nearby chests, carts and ships. The build menu counts them too, so a piece
shows as buildable when your chests have what it needs. Your inventory is always used
first, then the containers closest to the piece. Only building uses them, not crafting.

Chests are handled by **Chestworks**, which installs with Valchitecture. Its FAQ covers
which containers count and how it works with other storage mods.

## How far it reaches

| Level | Containers used |
|---|---|
| 15 | Within your build reach |
| 50 | Also in range of a workbench covering you or the piece |
| 75 | Also across linked workbenches |

Workbenches link when their ranges overlap. Any crafting station counts: a workbench,
forge and stonecutter that overlap form one network, and a chain of them reaches as far
as it goes.

## Getting materials back

From level 85 (Dvergr Thrift), when you remove a piece with the hammer, its materials go:

1. Into nearby containers that already hold that material, so your storage stays sorted.
2. Into your inventory, if there's room and you can carry it.
3. Anything left drops on the ground as usual.

You get back the same amount the game normally gives. Pieces that collapse or are
destroyed some other way drop their materials as usual.
