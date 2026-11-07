using System.Collections.Generic;
using ColoredDamageTypesMod.Common.Configs;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ColoredDamageTypesMod.Common.GlobalItems;

public class ItemChanges : GlobalItem
{
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        ColoredDamageTypesConfig config = ColoredDamageTypesConfig.Instance;
        if (!config.ChangeTooltipColor)
            return;

        foreach (TooltipLine line in tooltips)
        {
            Color? color = null;
            if (line.Name == "Defense")
                color = config.TooltipDefense;
            else if (line.Name == "Damage")
                color = DamageTypes.TooltipColorOf(item);

            if (config.DebugMode && config.DebugModeTooltips)
            {
                DamageTypes.Log("Item: " + item.Name);
                DamageTypes.Log("DmgType: " + DamageTypes.KeyOf(item.DamageType));
                foreach (string key in DamageTypes.CrossModDamageConfig.Keys)
                    DamageTypes.Log(key);
            }
            if (color is Color c && !(c.R == 255 && c.G == 255 && c.B == 255))
                line.OverrideColor = c;
        }
    }
}
