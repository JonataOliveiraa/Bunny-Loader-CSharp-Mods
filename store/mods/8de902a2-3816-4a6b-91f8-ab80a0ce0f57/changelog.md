## 2.0.0

Ported from JavaScript to C#. Packets are binary instead of JSON text. A phone that hosts a world now gets its own Death Teleport, messages and sounds, which the JavaScript version skipped for the host. Map icons are a map layer of their own instead of a hook on the ping layer, so they may draw in a different order against pings. Icon textures keep the game's default wrap and filter modes, because the C# API does not expose them. Teleports that version 1.0.0 kept in the mod's data folder are no longer imported, because C# mods have no such folder: worlds saved with 1.1.0 or later keep theirs.

# What's new

## 1.2.0

- Custom icon for each biome or purpose.
- Option to favorite teleports (they come first in the list).
- Filter for favorite teleports or by biome.

## 1.1.0

- New UI to manage and create teleports.

## 1.0.0

- First update.
- Teleport system.
