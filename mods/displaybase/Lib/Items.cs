using System;
using System.Globalization;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;

namespace DisplayBaseMod.Lib;

public static class Items
{
    public static Item NewItem(int type, int prefix = 0, int stack = 1)
    {
        Item item = Item.New(type, default);
        if (prefix > 0)
            item.prefix = (byte)prefix;
        item.stack = stack;
        return item;
    }

    // A game item's number, or "<mod>/<class>" for a mod item, whose number can change.
    public static object ItemKey(int type)
    {
        ModItem? modItem = ModContent.GetModItem(type);
        return modItem == null ? type : modItem.Mod.Name + "/" + modItem.Name;
    }

    public static int ItemFromKey(object? key)
    {
        switch (key)
        {
            case string text:
                return ModContent.TryFind(text, out ModItem found) ? found.Type : 0;
            case byte or short or int or long or float or double:
                return Convert.ToInt32(key, CultureInfo.InvariantCulture);
            default:
                return 0;
        }
    }

    public static int Drop(int x, int y, int type, int prefix, int stack)
    {
        if (type <= 0)
            return -1;
        return Item.NewItem(x * 16, y * 16, 48, 48, type, stack, false, prefix, default);
    }
}
