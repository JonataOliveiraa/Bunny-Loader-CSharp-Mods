## 2.0.0

Ported from JavaScript to C#, with every icon of the original. Mod.Call now takes C# delegates (Func<Item, DrawableTooltipLine, bool> and Action<Item, DrawableTooltipLine, SpriteBatch, Vector2, Color>), and a texture given as a string must be "ModName/path", because ModContent.Request needs the mod's name.

# Changelog

## 1.0.1

- The Expert, Master, research and boss summon icons are the game's own
  icons, scaled to the tooltip icon size.

## 1.0.0

- Port of Tooltip Icon 0.7.2 to Bunny Loader: every icon of the original,
  including prefix, size, movement speed, Expert, Master and research icons.
- Icons follow the mobile tooltip scale and fade.
- All Mod.Call methods, including `AddBossSummoningItem`.
