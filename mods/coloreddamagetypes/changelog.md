## 2.0.0

Ported from JavaScript to C#. The Pillar Colors and Mod Defaults config buttons are gone because C# configs have no buttons: use /cdt pillar and /cdt reset. Header colors and greyed-out rows are gone too, so the Held Item Class colors are always shown; a change there only counts while holding a weapon of a modded class. Colors of other mods' classes moved to coloreddamagetypes.CrossModDamageTypes.txt in the game's save folder, as plain text, because C# mods have no mod data folder and no JSON library: colors saved by the JavaScript version are not read. Mod.Call colors accept #RRGGBB, a Color or an array of three numbers. In multiplayer on a phone host (netMode 3), the host now passes hit packets on, shows its clients' numbers and announces its own hits; the JavaScript version tested netMode 1 and 2 only.

# Changelog

## 1.1.0

- New **Held Item Class** section in the settings: hold a weapon of a modded
  damage class to see and change its tooltip, damage and crit colors right in
  the menu. **Mod Defaults** brings back the colors the mod registered.
- Settings in 12 languages, with shorter labels.

## 1.0.1

- Colors sent by other mods through `Call('AddDamageType', ...)` always apply,
  even when they arrive late; only the colors you changed take their place.

## 1.0.0

- Port of *Colored Damage Types* (tModLoader) to Bunny Loader.
- The original's four configs on one screen; the Celestial Pillars preset is a
  button.
- Colors of other mods' damage classes are kept in `CrossModDamageTypes.json`.
