## 2.0.0

Ported from JavaScript to C#. The Clear My Lists button is gone from the config screen, because config buttons have no UI: use /beams clear. The player's lists are now in lootbeams.lists.txt in the game's save folder, as plain text, because C# mods have no mod_data folder and no JSON library: lists saved by the JavaScript version are not read. Min Value is stored by its coin label (1c, 1s, 1g...), so a Min Value saved by the JavaScript version goes back to the default.

# Changelog

## 1.1.2

- Items with a modded rarity get a beam in that rarity's color (animated
  colors too); unknown rarities above 11 use purple.
- The arrow beam no longer shows a thin dark line under its tip.

## 1.1.1

- Fixed: only coins, hearts and other listed items had beams; every item
  that passes the rarity and value filters has a beam again.

## 1.1.0

- Beams are drawn through the item's world drawing, like the original mod.
- Settings and messages in 12 languages, with shorter labels.

## 1.0.0

- Port of Loot Beams 1.9.4.5 for tModLoader to Bunny Loader: same colors,
  rules, lists, flash, pulse and textures.
- Full settings menu; the original's item lists became the `/beams` command,
  used with the item in hand.
- New: arrow beam styles and a pixel glow (textures the original shipped
  without using), light around the item and a pulse option.
