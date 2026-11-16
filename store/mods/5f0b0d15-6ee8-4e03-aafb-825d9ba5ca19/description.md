# Tooltip Icon

Adds a small icon to the left of each item tooltip line, like Tooltip Icon for
tModLoader (Nil256): damage by class, critical chance, speed, knockback,
defense, pickaxe, axe and hammer power, range, healing, mana, placeables,
ammo, consumables, equipables, buff time, materials, fishing, prefixes
(with + and −), Expert, Master and research lines.

The Expert, Master, research and boss summon icons are the game's own icons,
as in the original.

## Mod.Call

Recommended icon size: 22 × 22 px. Textures can be a `Texture2D`, an asset
(`ModContent.Request<Texture2D>`) or a `"ModName/path"` string.

```csharp
if (ModLoader.TryGetMod("tooltipicon", out Mod ti))
{
    ti.Call("AddNormalIcon", "mymod", "MyLine", ModContent.Request<Texture2D>("mymod/Assets/MyIcon"));
    ti.Call("AddPrefixIcon", "mymod", "MyPrefixLine", "mymod/Assets/MyIcon", false);
    ti.Call("AddFramedIcon", "mymod", "MyLine2", "mymod/Assets/Sheet", new Rectangle(0, 0, 22, 22), Vector2.Zero);
    ti.Call("AddCustomIcon", "mymod", "MyLine3", (Action<Item, DrawableTooltipLine, SpriteBatch, Vector2, Color>)((item, line, spriteBatch, position, color) => { }));
    ti.Call("AddDamageClassIcon", ModContent.GetInstance<MyDamageClass>(), "mymod/Assets/Damage", "mymod/Assets/Speed", null, null);
    ti.Call("AddConsumableIcon", (Func<Item, DrawableTooltipLine, bool>)((item, line) => item.type == myType), "mymod/Assets/MyConsumable");
    ti.Call("AddEquipableIcon", (Func<Item, DrawableTooltipLine, bool>)((item, line) => item.type == myType), "mymod/Assets/MyEquip");
    ti.Call("AddBuffTimeIcon", (Func<Item, DrawableTooltipLine, bool>)((item, line) => item.type == myType), "mymod/Assets/MyBuffTime");
    ti.Call("AddBossSummoningItem", ModContent.ItemType<MySummon>());
}
```

Call it from `PostSetupContent`.
