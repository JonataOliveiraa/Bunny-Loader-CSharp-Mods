## 2.0.0

Ported from JavaScript to C#. Structures, shops, prices and defaults are unchanged. Gnorp's mood lines go through a hook on ShopHelper.AddHappinessReportText, because the C# loader does not point the game's mood keys at a mod's texts; their {0} became {BiomeName} and {NPCName}. The party, shimmer and portrait sprites are declared in a town NPC profile instead of being found by file name. The food in the bar room's platters is written to TEFoodPlatter.item by field name, because that class has no C# proxy. The image opacity slider shows a plain number, because C# configs have no % suffix. The structure index is read by a small JSON reader in the mod, because the phone has no System.Text.Json.

# What's new

## 1.3.8.5

- Gnorp now uses the right sprite when sitting (the frame was one off on
  mobile).
- Translated into every Terraria language: German, French, Spanish, Italian,
  Russian, Polish and Chinese.

## 1.3.8.4

- The preview (image, dust outline and Anchor button) no longer stays on an
  empty slot after the last globe is used.

## 1.3.8.3

- New default anchor point: bottom center. The building rises from the block
  you tap, so you can see how far it goes. The four corners are still there.
- The touch button is now called Anchor.

## 1.3.8.2

- The building preview image is now drawn with the world (right after NPCs),
  using the game zoom: it was offset and followed the screen zoom.

## 1.3.8.1

- Port of Gnorp's Builder 1.3.8.1 to Bunny Loader.
- The 116 buildings were converted from the StructureHelper files and are built
  by the mod itself (no StructureHelper needed).
- Mobile: the corner is changed with the Anchor touch button (right click on PC)
  or in the settings; globes do not auto reuse; tooltip details always shown.
- Multiplayer: clients ask the server, which checks the request, builds and
  syncs the area.
