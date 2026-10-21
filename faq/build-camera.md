# How do the build camera and workbenches work together?

## Where the camera can go

The camera unlocks at level 50 and needs a crafting station to work from until level 100.

- **Level 50 to 74.** The camera needs you standing inside a station's range. It works
  from the station you stood in when it left you, and can fly within that station's range
  plus 5 m, or within its own range of you (7 m at level 50, 16 m at 74).
- **Level 75 (table hopping).** The working station is the one the camera is inside, or
  within 5 m of, so the camera can move from station to station. It can also fly within
  your build reach plus 5 m of you. Standing within 5 m of a station's edge is enough to
  keep the camera out.
- **Level 100.** No station is needed.

At every level the camera can rise to 1.5 times the working station's range above it, and
stays close enough to you that your character's area of the world stays loaded.

If you enter a mode below level 100 with no station available, the camera stays on your
character until you press the camera key, so walking into a station later does not pull
the camera away.

## Which station counts for building

Pieces that need a workbench or other station follow the game's rule: you must be
standing inside a station of that kind. This is your home station, and its ring shows
white; any other station the camera works from shows red.

From level 100, a station whose range reaches within 5 m of the camera also counts, so
you can build around a distant base. While you stand outside every station of that kind,
such pieces can only be placed inside a station's range; elsewhere the game reports a
missing requirement. The same rules apply to removing, moving in Edit mode, and area
repair.

## Reach

Build reach is 1.5 times the game's in a mode, and grows another 20% every ten levels from
60, to double at 100 (the `Longer Reach` setting). Reach is measured from your character,
as the game measures it. At level 100 with the camera at a station, you can build anywhere
inside that station's range instead.
