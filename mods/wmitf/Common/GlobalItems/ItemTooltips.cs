using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WMITFMod.Common.GlobalItems;

public class ItemTooltips : GlobalItem
{
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        ModItem? modItem = item.ModItem;
        if (modItem == null)
            return;

        // The JavaScript mod.name is the manifest name, which is DisplayName here.
        var line = new TooltipLine(Mod, "WMITF_" + Mod.DisplayName, "[" + modItem.Mod.DisplayName + "]");
        line.OverrideColor = Colors.RarityBlue;
        tooltips.Add(line);
    }
}
