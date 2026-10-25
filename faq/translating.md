# How do I translate the mod?

All the mod's text in game can be translated. The English is in the mod's
`Translations/English` folder: `valchitecture.json` for messages and hints, and
`Config.json` for the settings as the in-game settings window shows them.

1. Make a folder beside `English`, named with the game's own word for your language:
   `German`, `French`, `Spanish`, and so on.
2. Copy both files into it and translate the text on the right of each line.
3. Start the game. Your translation loads on its own.

Keep the names on the left exactly as they are. Keep any `$1` or `$2` too: the game fills
those in with a piece's name or a number, and you can move them anywhere your sentence
needs. Any line you leave out shows in English, so a partial translation works.

The in-game settings window shows each setting in your language. The `.cfg` file itself
stays in English, and so do key names like `Left Shift`, piece and mod names, and the log.
