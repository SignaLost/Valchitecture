# How does moving pieces in Edit mode work?

Select the hammer's Repair tool and press the mode key. Click a piece to pick it up; the
game's placement ghost then stands in for it, with rotation, nudging and snapping. Click
to set it down, or right-click to put it back.

## What can be moved

Picking up uses the game's removal checks: no-build zones, wards and the station rule.
A private chest that holds items can be moved, though the game will not let you remove
it. A container someone has open, or a ship with players aboard, cannot be moved. With
`Guard Used` on (the default), neither can a cart someone is pulling, a seat someone sits
in or a bed someone sleeps in; the hammer refuses to remove them too, in or out of a
mode. A station someone is crafting at is not protected, as the game only knows that on
the crafter's own machine.

Setting down uses the game's placement checks at the new spot, plus the station rule.

## What a move keeps and costs

The piece is recreated at the new spot from a full copy of its data, so contents,
smelter progress, its builder and its stored skill all carry over. A move costs the
stamina of two hammer swings and the tool wear of a removal plus a placement, and pays
0.75 experience.

While you hold a piece, it stays in place, hidden and without collision. Support updates
on your own pieces pause until you set it down or put it back, so what rests on it does
not fall.
