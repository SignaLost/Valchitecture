# Changelog

## 1.1.1

- Snap points only show in Auto and Manual. In Auto, the piece you're aiming at only
  shows its points when the piece you're holding has some.
- Fixed pieces getting stuck slightly crooked after turning, so they could never be
  straight again. Turning now always lands exactly straight when it passes a right angle.
- Resetting the rotation now turns the piece to the game's own angle closest to facing
  you: stairs and ramps climb away from you, and walls show you their broad side.
- Rotation steps now fit the game's own 22.5° turns: 2.8125°, 5.625°, 11.25°, 22.5°, 45°,
  67.5° and 90°, so pieces always line up with ones placed without the mod.
- `-` and `+` now change the nudge step while nudging, in finer steps from 5 cm to 1 m,
  and the rotation step otherwise. Hold `Shift` while nudging to change the rotation step. The `Rotation Step`, `Nudge Step` and
  `Step Rate Large Modifier` settings are gone.

## 1.1.0

- The Building skill levels up faster at higher levels, so the later abilities come sooner.
- Hammer's Call now unlocks at level 5, Gathered Hoard at 15, Roots of the Hall at 75 and
  Dvergr Thrift at 85. If you already have a config file, it keeps your old levels until
  you reset them.
- New options to lose less Building skill when you die, or to have it fortify.
- New option to show snap points on the piece you're holding and the piece you're aiming
  at. In Manual, the points you've picked turn green.
- In Manual, a snap point you leave on automatic now picks the closest point, the same way
  Auto does.
- Settings are reorganized into Abilities and Progression. Your existing settings carry
  over.

## 1.0.0

First release.

- **Building skill**, kept by Mimir. It rises as you place, move and repair pieces, and
  as you remove pieces you didn't build. Abilities stay unlocked once earned.
- **Build mode** (Hammer's Call, level 10): rotation rings, nudging, snap modes, longer
  reach, copying pieces, and a hint bar in the game's style.
- **Building from chests** (Gathered Hoard, level 20): building takes missing materials
  from nearby chests, and the build menu counts them. With Longhouse Stores (level 50),
  chests in your workbench's range count too, and with Roots of the Hall (level 100),
  chests across linked workbenches.
- **Edit mode** (Huginn's Counsel, level 25): pick up a built piece and set it down
  somewhere else, with everything it holds.
- **Area repair** (Eir's Touch, level 35): one repair swing repairs every damaged piece
  around you, as far as your stamina allows, at half the cost per piece.
- **Build camera** (Raven's View, level 50): a camera that leaves your character and flies
  around your workbench. With Bifröst Step (level 75) it can move between workbenches,
  and with Raven's Reach (level 100) you build where the camera is, no workbench needed.
- **Materials back** (Dvergr Thrift, level 100): a removed piece's materials go into your
  chests or inventory instead of dropping on the ground.
- **Snap modes**: Auto (the game's own), Manual (you pick the exact points), Grid and Off.
- **Extra snap points** on pieces while in a mode, plus hand-placed points for fences,
  furniture, item stands and more.
- **Terrain tools**: a height lock for leveling and smoothing, and a snap point so they
  can snap to a floor's edge.
- **Stronger pieces**: pieces built at a higher skill have more health and lose less
  support, and hammer swings cost less stamina. Repairing brings a piece up to your skill.
- **Building rules** that server owners can relax, all vanilla by default. `Guard Used`
  (on) stops anyone removing a piece another player is using, like an open chest or a bed.
- **Settings** to turn each ability on or off and change the level it unlocks at.
- **Per piece settings** for the snap points and support of every piece.
- A `valchitecture` console command that shows your skill, the piece you aim at, snap
  points and the server's settings.
- An API for other mods.
- All text can be translated, including the settings window.
- **Vanilla servers**: everything works without the mod on the server, except stronger
  pieces, repair upgrades and the building rules.
