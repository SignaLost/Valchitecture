# How do I translate the mod?

Everything the mod shows in game is behind translation keys, with the English in the
mod's `Translations/English` folder: `valchitecture.json` for messages and hints, and
`config.json` for the settings as the in-game config window shows them.

1. Make a folder beside `English` named with the game's own word for your language:
   `German`, `French`, `Spanish`, and so on.
2. Copy both files into it and translate the values.
3. Launch the game. Jotunn loads the file on its own; nothing else needs changing.

Keep the keys exactly as they are. Keep any `$1` or `$2` as well: those are filled in
with a piece's name or a number, and can move anywhere in the sentence your language
needs.

A key you leave out shows in English, so a partial translation works.

The config window shows each setting's name, description and section in your language.
The `.cfg` file itself stays in English, since it is written before translations load
and is shared between players and servers. Key names like `Left Shift`, piece and mod
names, and log lines also stay in English.
