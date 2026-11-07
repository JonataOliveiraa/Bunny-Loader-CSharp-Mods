## 2.0.0

Ported from JavaScript to C#. Item, tile and wall classes, numbers, recipes, shops, sprites and sounds are unchanged. Shop additions go through GlobalNPC.ModifyShop, because C# has no SetupShop; as NpcType is the Painter for both of his shops, the Painter's items appear in both his main and decoration shops. The Chain Link Fence wall still has no item, as in the original, which names an item that does not exist. The description images moved from assets/ to Assets/, because the sounds' folder has that name once the package files sit at the mod's root.

# News

## 1.0.3 - 2026-10-10

- Updated tooltips for some items
- Added missing recipes
- Added Spanish and Russian translations
- The maxStack for wall items has been set to 9999

## 1.0.2 - 2026-10-09

- Some items can now be obtained from NPC shops.
- Updated Description

## 1.0.0 - 2026-10-04

- Release version, ported from Terrarchitect 1.3.
