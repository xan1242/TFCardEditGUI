# Yu-Gi-Oh! Tag Force CardInfo Editor GUI

This is a frontend for [TFCardEdit](https://github.com/xan1242/TFCardEdit) which allows you to edit the card database found in Yu-Gi-Oh! Tag Force games, Yu-Gi-Oh! Online games and all of the Yu-Gi-Oh! titles developed by Other Ocean Interactive.

**Currently only supports Tag Force 1**. This will expand to the other mentioned games in the future.

## Features

You can edit the following card properties:

- names
- descriptions
- levels
- frame
- ATK and DEF
- type
- attribute
- rarity
- password
- game exclusivity flag

You can also *technically* add cards manually in the .ini file. 

**HOWEVER:** these cards will **not** have their effects unless it specifically exists in the game logic/code.

You **can** add normal monsters which will work fine. You must also provide the game with card art, which you can do with my [CIPTool](https://github.com/xan1242/CIPTool) utility. (this will be implemented later directly into this app as well)

## TODO list (backend)

- support for Huffman compression/decompression (Tag Force 2+)
- support for other games and their card prop formats
- figure out the Genre, SamePict, Sort and Top files
- dialog box support (DLG_Text)
- old format support (Yu-Gi-Oh! Power of Chaos and Online 1)
- YDC decklist editing (maybe frontend, maybe backend, depends on the format complexity)
- limited & forbidden list editing

## TODO list (frontend)

- add more visual flair (card frame colors to listed items, add icons, etc.)
- adding cards to the DB
- card ID reordering
- multi selection
- edit history and undo/redo
- copy paste
- import/export of a single card
- card art drawing, unpacking and repacking (by utilizing CIPTool and GIMConv library)
- Japanese text drawing and RegEx - this might call for a custom font renderer
- support for other card frames and pend scales
- YDC decklist editing - accompanying frontend code
- website crawler for Konami Card DB - this would allow to pull the latest card info straight from Konami (maybe use curl for this and just parse the html - this is just an idea)
- a faster ListView
- code cleanup and refactoring

## The magic behind this utility

- [TFCardEdit](https://github.com/xan1242/TFCardEdit) - the actual backend code to this utility which converts the database binary files back and forth to/from a text .ini file
- [ehppack](https://github.com/xan1242/ehppack) - EhFolder archiving utility which can unpack/repack the EhFolder achives found in Tag Force games allowing for seamless access to the card database
- And in the future (when card art gets added) - [CIPTool](https://github.com/xan1242/CIPTool) - utility for unpacking/repacking the card images for Tag Force games

## Third party stuff

- [ini-parser](https://github.com/rickyah/ini-parser) by [rickyah](https://github.com/rickyah) (MIT)
- [ObjectListView](http://objectlistview.sourceforge.net/cs/index.html) by Phillip Piper (GPLv3)

## Misc

This is my first actual UI application and my very first C# program (from a mostly C/C++ dev). 

It is very messy as I've tried to push it to an usable state first, so please excuse any code spaghetti.

In the future I'd love to work in a different UI library, but for now WinForms has to do!