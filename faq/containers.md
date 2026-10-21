# How does building from containers work?

Building from containers runs on **Chestworks**, which Valchitecture installs with it.
Chestworks decides which containers count, keeps changes safe in multiplayer and works
with other storage mods; its FAQ covers those. Valchitecture decides how far building
reaches, and what removal pickup does.

## How far it reaches

| Level | Containers used |
|---|---|
| 20 | Within your build reach: the mode's reach in Build mode, the game's otherwise |
| 50 | Also within the range of a station covering you or the piece being placed |
| 100 | Also within every station linked to those, where two stations link when their ranges overlap |

Any kind of station links: a workbench, forge and stonecutter whose ranges overlap form one
network, and a chain of them reaches as far as it goes. Ranges include the server's
`Station Range` setting. In Chestworks this is the `building` purpose.

## Taking order

Your inventory is used first. Containers cover only what it lacks, nearest to the piece
first. The build menu, the piece's requirement list and the "can build" check all count
the containers, so a piece shows as buildable when they cover it.

Only building uses containers; crafting at a station does not.

## Removal pickup

From level 100, a piece you remove with the hammer gives its materials back in this order:

1. Containers in the same reach that already hold that material, nearest to the piece
   first. A container without it is never used, so storage stays sorted. This step
   needs Gathered Hoard on.
2. Your inventory, within its free space and your carry weight, as the game's own auto
   pickup allows.
3. The rest drops where the piece was, as the game drops it, for anyone to pick up.

Materials taken in steps 1 and 2 never appear in the world, so no one else can pick them up
first. The amounts are the game's own: the reduced return from pieces no player built,
partly eaten feasts, and pieces that drop a bag all work as usual. Pieces that collapse or
are destroyed some other way drop as usual.

A piece you remove is taken over first, so its materials arrive on your side. The
exception is a piece holding a container, such as a chest, cart or ship, that another
player's game controls: its contents are current only on their side, so its removal stays
with them, and its materials drop as usual.
