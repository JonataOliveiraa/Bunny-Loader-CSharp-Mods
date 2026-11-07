# Legendary Difficulty

Adds **custom world difficulties** to Terraria. The game only has Normal, Expert,
Master and Journey; this mod adds new options to the world creation menu, and
comes with **Legendary**.

## Contents

- **Difficulty system**: registers new difficulties, with ids after the game's
  four.
- **World creation menu**: the new difficulty appears in the difficulty list,
  with its icon, preview image and color.
- **World selection menu**: the list of worlds shows the custom difficulty's
  name and color.
- **Compatibility**: `Main.Difficulty` and `Main.GameMode` are hooked so the game
  runs a custom difficulty as the vanilla one it stands for, for example Master
  loot with stronger scaling.

## Legendary

A difficulty that behaves as Master, with difficulty level 4. Its name fades
between green and white.

## Notes

- A custom difficulty is stored in the world's `GameMode` value: anything above
  the game's four is a mod difficulty.
- It can only be chosen when creating a world: existing worlds cannot change.
- Characters in Journey mode cannot pick it, and cannot play those worlds.
- Ported from the TL Pro version.

## Author

GST378 ([YouTube](https://youtube.com/@gst_378))
